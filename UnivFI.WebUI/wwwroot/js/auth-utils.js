// 인증 유틸리티 함수
// 페이지 로드 시 인증 상태 확인 및 UI 업데이트
$(document).ready(function () {
  console.log("auth-utils.js: 페이지 로드됨");
  
  // localStorage 값 로그 출력 (디버깅용)
  console.log("auth-utils.js: JWT 토큰 존재 여부:", !!localStorage.getItem("jwt_token"));
  console.log("auth-utils.js: 사용자 정보 존재 여부:", !!localStorage.getItem("user_info"));
  
  // 로컬 스토리지에서 토큰 확인
  const token = localStorage.getItem("jwt_token");
  const user = JSON.parse(localStorage.getItem("user_info") || "null");

  // 토큰 만료 여부 확인 (선택적)
  checkTokenExpiry();

  // AJAX 요청에 인증 토큰 자동 추가 설정
  setupAuthInterceptors();

  // 인증 상태 업데이트 - 토큰이 있으면 로그인 상태로 간주
  updateAuthState(!!token, user);

  // 토큰이 있으면 자동 로그인 시도 (API 검증)
  if (token) {
    console.log("auth-utils.js: 토큰 있음, 유효성 검증 시도");
    verifyTokenAndUpdateUI();
  }

  // 로그아웃 버튼 클릭 이벤트 처리
  $("#logoutButton").on("click", function (e) {
    e.preventDefault();
    logout();
  });

  // 로그아웃 폼 제출 이벤트
  $("#logoutForm").on("submit", function (e) {
    e.preventDefault(); // 기본 제출 방지
    console.log("auth-utils.js: 로그아웃 폼 제출");
    logout();
  });

  // 인증이 필요한 링크에 대한 처리
  $("[data-requires-auth='true']").on("click", function (e) {
    e.preventDefault();
    var returnUrl = $(this).data("return-url") || window.location.href;
    var requiresAdmin = $(this).data("requires-admin") === true;

    handleAuthenticatedNavigation(returnUrl, requiresAdmin);
  });

  // 권한 기반 UI 요소 갱신
  updateUIByPermission();
});

// 토큰 만료 확인 함수
function checkTokenExpiry() {
  const tokenExpiry = localStorage.getItem("token_expiry");
  
  if (!tokenExpiry) {
    return;  // 만료 시간 정보가 없으면 확인 불가
  }
  
  try {
    // 만료 시간을 Date 객체로 변환
    const expiryDate = new Date(tokenExpiry);
    const now = new Date();
    
    console.log("auth-utils.js: 토큰 만료 시간 확인", {
      expiry: expiryDate.toISOString(),
      now: now.toISOString(),
      isExpired: now > expiryDate
    });
    
    if (now > expiryDate) {
      console.log("auth-utils.js: 토큰이 만료되었습니다. 로그아웃 처리.");
      logout(false);
      return false;
    }
    
    return true;
  } catch (e) {
    console.error("auth-utils.js: 토큰 만료 시간 확인 오류", e);
    return true; // 오류 발생 시 토큰이 유효하다고 가정
  }
}

// 로그인 처리 함수 - Login.cshtml에서는 processLogin()으로 처리됨
function login() {
  const username = $("#username").val();
  const password = $("#password").val();

  if (!username || !password) {
    showLoginError("사용자 이름과 비밀번호를 모두 입력해주세요.");
    return;
  }

  // 로그인 버튼 비활성화 및 로딩 표시
  const loginButton = $("#loginButton");
  const originalText = loginButton.html();
  loginButton
    .prop("disabled", true)
    .html('<i class="bi bi-hourglass-split"></i> 로그인 중...');

  // API 요청
  $.ajax({
    url: "/api/auth/login",
    type: "POST",
    contentType: "application/json",
    data: JSON.stringify({
      username: username,
      password: password,
      rememberMe: $('#rememberMe').is(':checked')
    }),
    success: function (response) {
      console.log("auth-utils.js: 로그인 성공 응답", response);
      
      // 응답 형식에 따라 유연하게 처리
      const token = response.token || "";
      const refreshToken = response.refreshToken || "";
      const tokenExpiry = response.tokenExpiry || "";
      const userInfo = response.userInfo || response.user || {};
      
      // 데이터 저장 시 디버깅 로그
      console.log("auth-utils.js: 토큰 저장 전:", {token: !!token, hasUserInfo: !!userInfo});
      
      // localStorage에 저장
      localStorage.setItem("jwt_token", token);
      if (refreshToken) localStorage.setItem("refresh_token", refreshToken);
      if (tokenExpiry) localStorage.setItem("token_expiry", tokenExpiry);
      localStorage.setItem("user_info", JSON.stringify(userInfo));
      
      // 저장 후 확인
      console.log("auth-utils.js: 토큰 저장 후:", {
        tokenSaved: !!localStorage.getItem("jwt_token"),
        userInfoSaved: !!localStorage.getItem("user_info")
      });

      // UI 업데이트
      updateAuthState(!!token, userInfo);

      // 리디렉션
      if (response.redirectUrl) {
        console.log("auth-utils.js: 리디렉션 URL로 이동:", response.redirectUrl);
        window.location.href = response.redirectUrl;
      } else {
        console.log("auth-utils.js: 홈으로 이동");
        window.location.href = "/";
      }
    },
    error: function (xhr) {
      let errorMessage = "로그인에 실패했습니다.";
      if (xhr.responseJSON && xhr.responseJSON.message) {
        errorMessage = xhr.responseJSON.message;
      }
      console.error("auth-utils.js: 로그인 오류", errorMessage);
      showLoginError(errorMessage);

      // 로그인 버튼 복원
      loginButton.prop("disabled", false).html(originalText);
    },
  });
}

// 로그인 에러 표시 - Login.cshtml과 일치하도록 요소 ID 변경
function showLoginError(message) {
  $("#apiErrorMessage").text(message).removeClass("hidden");
}

// 토큰 유효성 검증 및 UI 업데이트
function verifyTokenAndUpdateUI() {
  const token = localStorage.getItem("jwt_token");
  if (!token) {
    console.log("auth-utils.js: 토큰 없음, 검증 스킵");
    return;
  }

  console.log("auth-utils.js: 토큰 검증 시작");
  $.ajax({
    url: "/api/auth/verify",
    type: "GET",
    headers: {
      Authorization: "Bearer " + token,
    },
    success: function (response) {
      console.log("auth-utils.js: 토큰 검증 성공", response);
      
      // 유효한 토큰이면 사용자 정보 업데이트
      if (response.success && response.user) {
        localStorage.setItem("user_info", JSON.stringify(response.user));
        updateAuthState(true, response.user);
      } else {
        console.warn("auth-utils.js: 토큰은 유효하지만 사용자 정보가 없습니다");
      }
    },
    error: function (xhr) {
      console.error("auth-utils.js: 토큰 검증 실패", xhr.status);
      
      // 인증 오류(401) 또는 서버 오류(500)의 경우 로그아웃 처리
      if (xhr.status === 401) {
        console.log("auth-utils.js: 토큰이 유효하지 않습니다. 로그아웃 처리.");
        logout(false);
      }
    },
  });
}

// 인증 상태에 따른 UI 업데이트
function updateAuthState(isLoggedIn, user) {
  console.log("auth-utils.js: 인증 상태 업데이트", { isLoggedIn, user });
  
  if (isLoggedIn && user) {
    // 로그인 상태 UI 표시
    $(".auth-logged-in").removeClass("hidden");
    $(".auth-logged-out").addClass("hidden");

    // 사용자 정보 표시
    $(".auth-user-name").text(user.name || user.username || "사용자");
    $(".auth-user-email").text(user.email || "");
    $(".auth-user-role").text(user.role || "");

    // 관리자 메뉴 표시/숨김
    if (userHasRole("Admin") || userHasRole("Administrators")) {
      $(".admin-menu-buttons").removeClass("hidden");
      $(".non-admin-menu-buttons").addClass("hidden");
    } else {
      $(".admin-menu-buttons").addClass("hidden");
      $(".non-admin-menu-buttons").removeClass("hidden");
    }
  } else {
    // 로그아웃 상태 UI 표시
    $(".auth-logged-in").addClass("hidden");
    $(".auth-logged-out").removeClass("hidden");
    $(".admin-menu-buttons").addClass("hidden");
    $(".non-admin-menu-buttons").removeClass("hidden");

    // 사용자 정보 초기화
    $(".auth-user-name").text("사용자");
    $(".auth-user-email").text("");
    $(".auth-user-role").text("");
  }
}

// 로그아웃 처리
function logout(redirectToLogin = true) {
  console.log("auth-utils.js: 로그아웃 처리 시작", { redirectToLogin });
  
  const token = localStorage.getItem("jwt_token");
  
  // 서버 API 호출하여 로그아웃 처리
  if (token) {
    console.log("auth-utils.js: 서버 로그아웃 API 호출");
    $.ajax({
      url: "/api/auth/logout",
      type: "POST",
      headers: {
        Authorization: "Bearer " + token
      },
      success: function(response) {
        console.log("auth-utils.js: 서버 로그아웃 성공", response);
        completeLogout(redirectToLogin);
      },
      error: function(xhr, status, error) {
        console.warn("auth-utils.js: 서버 로그아웃 API 오류", {status: xhr.status, error: error});
        // API 오류가 발생해도 로컬 데이터는 삭제 처리
        completeLogout(redirectToLogin);
      },
      timeout: 5000 // 5초 타임아웃 설정
    });
  } else {
    // 토큰이 없는 경우 로컬 데이터만 정리
    console.log("auth-utils.js: 토큰 없음, 로컬 데이터만 삭제");
    completeLogout(redirectToLogin);
  }
}

// 로그아웃 완료 처리 (로컬 데이터 삭제 및 리디렉션)
function completeLogout(redirectToLogin) {
  // 토큰 및 사용자 정보 삭제 (확실히 삭제되도록 직접 삭제 추가)
  clearJwtData();
  
  // 삭제 확인을 위한 디버깅 로그
  console.log("auth-utils.js: 로그아웃 후 localStorage 확인", {
    jwt_token: localStorage.getItem("jwt_token"),
    refresh_token: localStorage.getItem("refresh_token"),
    token_expiry: localStorage.getItem("token_expiry"),
    user_info: localStorage.getItem("user_info")
  });

  // UI 업데이트
  updateAuthState(false, null);

  // 로그인 페이지로 리디렉션 (필요시)
  if (redirectToLogin) {
    console.log("auth-utils.js: 로그인 페이지로 리디렉션");
    window.location.href = "/Account/Login";
  }
}

// JWT 데이터 초기화 - Login.cshtml에서 호출함
function clearJwtData() {
  console.log("auth-utils.js: JWT 데이터 초기화 호출됨");
  
  try {
    // 모든 항목 개별적으로 삭제
    localStorage.removeItem("jwt_token");
    localStorage.removeItem("refresh_token");
    localStorage.removeItem("token_expiry");
    localStorage.removeItem("user_info");
    
    // 세션 스토리지도 정리 (필요 시)
    sessionStorage.removeItem("jwt_token");
    sessionStorage.removeItem("user_info");
    
    // 삭제 직후 확인
    console.log("auth-utils.js: JWT 데이터 초기화 완료", {
      jwt_token_removed: !localStorage.getItem("jwt_token"),
      user_info_removed: !localStorage.getItem("user_info")
    });
    
    // 여전히 삭제가 안 된 경우 추가 시도
    if (localStorage.getItem("jwt_token") || localStorage.getItem("user_info")) {
      console.warn("auth-utils.js: 첫 번째 시도로 localStorage 항목이 삭제되지 않음, 다시 시도");
      
      // 다른 방식으로 다시 시도
      window.localStorage.removeItem("jwt_token");
      window.localStorage.removeItem("refresh_token");
      window.localStorage.removeItem("token_expiry");
      window.localStorage.removeItem("user_info");
      
      console.log("auth-utils.js: 두 번째 시도 후 확인", {
        jwt_token_removed: !localStorage.getItem("jwt_token"),
        user_info_removed: !localStorage.getItem("user_info")
      });
    }
  } catch (e) {
    console.error("auth-utils.js: localStorage 항목 삭제 중 오류 발생", e);
  }
}

// 인증이 필요한 페이지 네비게이션 처리
function handleAuthenticatedNavigation(url, requiresAdmin = false) {
  console.log("auth-utils.js: 인증 필요 페이지 처리", { url, requiresAdmin });
  
  const token = localStorage.getItem("jwt_token");
  const userInfo = localStorage.getItem("user_info");

  // 토큰이 없으면 로그인 페이지로 리디렉션
  if (!token) {
    window.location.href = "/Account/Login?returnUrl=" + encodeURIComponent(url);
    return;
  }

  // 관리자 권한 확인이 필요한 경우
  if (requiresAdmin) {
    const user = JSON.parse(userInfo || "null");
    if (!user || !userHasRole("Admin", user)) {
      alert("관리자 권한이 필요합니다.");
      return;
    }
  }

  // 권한이 확인되었으므로 페이지로 이동
  window.location.href = url;
}

// API 요청 헬퍼 함수 (토큰 자동 포함)
function apiRequest(url, method, data, successCallback, errorCallback) {
  const token = localStorage.getItem("jwt_token");

  let requestConfig = {
    url: url,
    type: method,
    headers: {},
    success: successCallback,
    error:
      errorCallback ||
      function (xhr) {
        console.error("API 요청 실패:", xhr);

        if (xhr.status === 401) {
          // 인증 오류시 로그아웃 처리
          logout(true);
        }
      },
  };

  // 토큰이 있으면 헤더에 추가
  if (token) {
    requestConfig.headers.Authorization = "Bearer " + token;
  }

  // 데이터가 있고 GET이 아닌 경우, JSON 형식으로 전송
  if (data && method.toUpperCase() !== "GET") {
    requestConfig.contentType = "application/json";
    requestConfig.data = JSON.stringify(data);
  }

  // AJAX 요청 실행
  $.ajax(requestConfig);
}

// 사용자 역할 확인 함수
function userHasRole(role, specificUser = null) {
  const user = specificUser || JSON.parse(localStorage.getItem("user_info") || "null");
  if (!user || !user.roles) return false;

  if (Array.isArray(user.roles)) {
    return user.roles.includes(role);
  } else if (typeof user.roles === "string") {
    // 역할이 문자열로 저장된 경우 (쉼표로 구분된 형식)
    return user.roles.split(",").map(r => r.trim()).includes(role);
  }
  
  return false;
}

// UI 요소 권한 기반 표시/숨김
function updateUIByPermission() {
  // 특정 역할별 UI 요소 제어
  $("[data-role-required]").each(function () {
    const requiredRole = $(this).data("role-required");
    if (!userHasRole(requiredRole)) {
      $(this).remove();
    }
  });
}

// HTTP 요청에 자동으로 인증 토큰 추가
function setupAuthInterceptors() {
  const token = localStorage.getItem("jwt_token");
  if (!token) return;

  // jQuery AJAX 요청에 헤더 추가
  $.ajaxSetup({
    beforeSend: function (xhr) {
      xhr.setRequestHeader("Authorization", "Bearer " + token);
    },
  });
}
