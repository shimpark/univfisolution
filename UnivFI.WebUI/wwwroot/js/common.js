/**
 * UnivFI 애플리케이션의 공통 자바스크립트 함수 모음
 */

/**
 * 검색 관련 기능
 */
const SearchUtils = {
  /**
   * 검색 기능 초기화
   * @param {Object} options - 초기화 옵션
   * @param {string} options.formSelector - 검색 폼 선택자
   * @param {string} options.inputSelector - 검색어 입력 필드 선택자
   * @param {string} options.fieldsInputSelector - 검색 필드 히든 인풋 선택자
   * @param {string} options.dropdownSelector - 드롭다운 버튼 선택자
   * @param {string} options.dropdownMenuSelector - 드롭다운 메뉴 선택자
   * @param {string} options.checkboxSelector - 검색 필드 체크박스 선택자
   * @param {string} options.defaultFields - 기본 검색 필드 (쉼표로 구분)
   * @param {string} options.currentPageSelector - 현재 페이지 히든 인풋 선택자
   */
  init: function (options) {
    const {
      formSelector = "#searchForm",
      inputSelector = 'input[name="searchTerm"]',
      fieldsInputSelector = "#searchFieldsInput",
      dropdownSelector = "#searchDropdown",
      dropdownMenuSelector = "#searchDropdownMenu",
      checkboxSelector = ".search-field-checkbox",
      defaultFields = "Name,Email",
      currentPageSelector = "#currentPage",
    } = options;

    // 검색어가 있을 경우 검색 입력 상자에 포커스
    const searchInput = $(inputSelector);
    if (searchInput.val()) {
      searchInput.focus();
    }

    // 검색 필드 체크박스 초기화
    const searchFields = $(fieldsInputSelector).val() || defaultFields;
    const fieldArray = searchFields.split(",");

    // 체크박스 상태 설정
    $(checkboxSelector).each(function () {
      const field = this.id.replace("check", "");
      $(this).prop("checked", fieldArray.includes(field));
    });

    // 체크박스 변경 이벤트
    $(checkboxSelector).change(function () {
      SearchUtils.updateSearchFields(checkboxSelector, fieldsInputSelector);
    });

    // 검색 드롭다운 토글
    $(dropdownSelector).click(function () {
      $(dropdownMenuSelector).toggleClass("hidden");
    });

    // 드롭다운 외부 클릭 시 닫기
    $(document).click(function (e) {
      if (
        !$(e.target).closest(dropdownSelector + ", " + dropdownMenuSelector)
          .length
      ) {
        $(dropdownMenuSelector).addClass("hidden");
      }
    });

    // 검색 폼 제출 시 페이지를 1로 설정
    $(formSelector).on("submit", function () {
      $(currentPageSelector).val(1);
    });
  },

  /**
   * 검색 필드 업데이트
   * @param {string} checkboxSelector - 체크박스 선택자
   * @param {string} fieldsInputSelector - 필드 입력 선택자
   */
  updateSearchFields: function (checkboxSelector, fieldsInputSelector) {
    const selectedFields = [];
    $(checkboxSelector + ":checked").each(function () {
      selectedFields.push(this.id.replace("check", ""));
    });
    $(fieldsInputSelector).val(selectedFields.join(","));
  },
};

/**
 * 페이지네이션 관련 기능
 */
const PaginationUtils = {
  /**
   * 페이징 기능 초기화
   * @param {Object} options - 초기화 옵션
   * @param {string} options.controller - 컨트롤러 이름
   * @param {string} options.area - 영역 이름 (옵션)
   * @param {string} options.linkSelector - 페이징 링크 선택자
   * @param {Function} options.loadPageCallback - 페이지 로드 콜백 함수
   */
  init: function (options) {
    const {
      controller,
      area = "",
      linkSelector = ".pagination-link",
      loadPageCallback,
    } = options;

    // 페이징 링크 클릭 이벤트 처리
    $(document).on("click", linkSelector, function (e) {
      e.preventDefault();
      const page = $(this).data("page");
      const linkController = $(this).data("controller");

      if (linkController !== controller) return; // 현재 컨트롤러에서만 동작

      if (typeof loadPageCallback === "function") {
        loadPageCallback(page);
      }
    });
  },

  /**
   * 페이지 로드 및 업데이트
   * @param {Object} options - 로드 옵션
   * @param {string} options.url - 요청 URL
   * @param {string} options.area - 영역 이름 (옵션)
   * @param {number} options.page - 페이지 번호
   * @param {string} options.pageSize - 페이지 크기
   * @param {string} options.searchTerm - 검색어
   * @param {string} options.searchFields - 검색 필드
   * @param {string} options.tableBodySelector - 테이블 본문 선택자
   * @param {string} options.paginationSelector - 페이징 컴포넌트 선택자
   * @param {string} options.paginationInfoSelector - 페이징 정보 선택자
   * @param {string} options.currentPageSelector - 현재 페이지 히든 인풋 선택자
   * @param {Object} options.additionalParams - 추가 파라미터들 (옵션)
   */
  loadPage: function (options) {
    const {
      url,
      area = "",
      page,
      pageSize,
      searchTerm,
      searchFields,
      tableBodySelector = "#dataTableBody",
      paginationSelector = "#paginationContainer",
      paginationInfoSelector = "#paginationInfoContainer",
      currentPageSelector = "#currentPage",
    } = options;

    // 로딩 표시
    $(tableBodySelector).html(
      '<tr><td colspan="5" class="text-center py-4"><i class="fas fa-spinner fa-spin mr-2"></i> 로딩 중...</td></tr>'
    );

    // 데이터 객체 생성
    const data = {
      page: page,
      pageSize: pageSize,
      searchTerm: searchTerm,
      searchFields: searchFields,
      area: area,
    };

    // options 객체에서 기본 파라미터가 아닌 모든 추가 파라미터를 data 객체에 추가
    const excludedParams = [
      "url",
      "area",
      "page",
      "pageSize",
      "searchTerm",
      "searchFields",
      "tableBodySelector",
      "paginationSelector",
      "paginationInfoSelector",
      "currentPageSelector",
    ];

    for (const key in options) {
      if (!excludedParams.includes(key)) {
        data[key] = options[key];
      }
    }

    // 컨트롤러 호출
    $.ajax({
      url: url,
      type: "GET",
      data: data,
      success: function (response) {
        // HTML 응답을 파싱하기 위한 임시 요소 생성
        const tempDiv = $("<div>").html(response);

        // 테이블 본문 업데이트
        const newTableBody = tempDiv.find(tableBodySelector).html();
        $(tableBodySelector).html(newTableBody);

        // 페이징 컴포넌트 업데이트
        const newPagination = tempDiv.find(paginationSelector).html();
        $(paginationSelector).html(newPagination);

        // 페이징 정보 업데이트
        const newPaginationInfo = tempDiv.find(paginationInfoSelector).html();
        $(paginationInfoSelector).html(newPaginationInfo);

        // 브라우저 URL 업데이트 (페이지 새로고침 없이)
        PaginationUtils.updateBrowserUrl(
          page,
          searchTerm,
          searchFields,
          area,
          pageSize,
          data
        );

        // 히든 필드 업데이트
        $(currentPageSelector).val(page);

        // 임시 요소 제거
        tempDiv.remove();
      },
      error: function (xhr, status, error) {
        // 오류 표시
        $(tableBodySelector).html(
          `<tr><td colspan="5" class="text-center py-4 text-red-600"><i class="fas fa-exclamation-circle mr-2"></i> 데이터를 불러오는 데 실패했습니다: ${error}</td></tr>`
        );
        console.error("페이지 로드 오류:", error);
      },
    });
  },

  /**
   * URL 업데이트 함수
   * @param {number} page - 페이지 번호
   * @param {string} searchTerm - 검색어
   * @param {string} searchFields - 검색 필드
   * @param {string} area - 영역 이름 (옵션)
   * @param {string} pageSize - 페이지 크기 (옵션)
   * @param {Object} additionalParams - 추가 파라미터 (옵션)
   */
  updateBrowserUrl: function (
    page,
    searchTerm,
    searchFields,
    area = "",
    pageSize = "",
    additionalParams = {}
  ) {
    const url = new URL(window.location.href);
    url.searchParams.set("page", page);

    if (searchTerm) {
      url.searchParams.set("searchTerm", searchTerm);
    } else {
      url.searchParams.delete("searchTerm");
    }

    if (searchFields) {
      url.searchParams.set("searchFields", searchFields);
    } else {
      url.searchParams.delete("searchFields");
    }

    if (area) {
      url.searchParams.set("area", area);
    } else {
      url.searchParams.delete("area");
    }

    // 페이지 크기 처리
    if (pageSize) {
      url.searchParams.set("pageSize", pageSize);
    } else {
      url.searchParams.delete("pageSize");
    }

    // 추가 파라미터 처리
    if (additionalParams) {
      for (const key in additionalParams) {
        // 기본 파라미터는 이미 처리했으므로 건너뜀
        if (
          ["page", "searchTerm", "searchFields", "area", "pageSize"].includes(
            key
          )
        )
          continue;

        const value = additionalParams[key];
        if (value) {
          url.searchParams.set(key, value);
        } else {
          url.searchParams.delete(key);
        }
      }
    }

    window.history.pushState({ path: url.href }, "", url.href);
  },
};

/**
 * 모달 관련 기능
 */
const ModalUtils = {
  /**
   * 삭제 확인 모달 열기
   * @param {Object} options - 모달 옵션
   * @param {string} options.id - 삭제할 항목 ID
   * @param {string} options.name - 삭제할 항목 이름
   * @param {string} options.page - 현재 페이지
   * @param {string} options.searchTerm - 검색어
   * @param {string} options.searchFields - 검색 필드
   * @param {string} options.controller - 컨트롤러 이름
   * @param {string} options.action - 액션 이름 (기본값: DeleteConfirmed)
   * @param {string} options.modalId - 모달 엘리먼트 ID (기본값: deleteModal)
   * @param {string} options.titleId - 모달 제목 엘리먼트 ID (기본값: deleteModalTitle)
   * @param {string} options.messageId - 모달 메시지 엘리먼트 ID (기본값: deleteModalMessage)
   * @param {string} options.formId - 모달 폼 엘리먼트 ID (기본값: deleteForm)
   * @param {string} options.entityType - 엔티티 타입 (기본값: 항목)
   * @param {string} options.termFieldId - 검색어 필드 ID (기본값: deleteTerm)
   * @param {string} options.fieldsFieldId - 검색 필드 ID (기본값: deleteFields)
   */
  openDeleteModal: function (options) {
    const {
      id,
      name,
      page,
      searchTerm,
      searchFields,
      controller,
      action = "Delete",
      modalId = "deleteModal",
      titleId = "deleteModalTitle",
      messageId = "deleteModalMessage",
      formId = "deleteForm",
      entityType = "항목",
      termFieldId = "deleteTerm",
      fieldsFieldId = "deleteFields",
    } = options;

    document.getElementById(titleId).textContent = `${entityType} 삭제 확인`;
    document.getElementById(
      messageId
    ).textContent = `${entityType} "${name}"을(를) 삭제하시겠습니까?`;
    document.getElementById(formId).action = `/${controller}/${action}/${id}`;

    if (document.getElementById(termFieldId)) {
      document.getElementById(termFieldId).value = searchTerm || "";
    }

    if (document.getElementById(fieldsFieldId)) {
      document.getElementById(fieldsFieldId).value = searchFields || "";
    }

    document.getElementById(modalId).classList.remove("hidden");
    document.getElementById(modalId).classList.add("flex");
  },

  /**
   * 모달 닫기
   * @param {string} modalId - 모달 엘리먼트 ID
   */
  closeModal: function (modalId) {
    document.getElementById(modalId).classList.add("hidden");
    document.getElementById(modalId).classList.remove("flex");
  },

  /**
   * 오류 모달 표시
   * @param {string} message - 오류 메시지
   * @param {string} modalId - 모달 ID (기본값: dynamicErrorModal)
   */
  showErrorModal: function (message, modalId = "dynamicErrorModal") {
    const modal = document.createElement("div");
    modal.className =
      "fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50";
    modal.id = modalId;

    const content = `
            <div class="bg-white rounded-lg p-6 max-w-md w-full">
                <div class="mb-4">
                    <h3 class="text-xl font-semibold text-red-600">오류 발생</h3>
                </div>
                <div class="mb-6">
                    <p class="text-gray-700">${message}</p>
                </div>
                <div class="flex justify-end">
                    <button type="button" onclick="ModalUtils.closeErrorModal('${modalId}')"
                        class="px-4 py-2 bg-gray-300 text-gray-700 rounded hover:bg-gray-400">
                        닫기
                    </button>
                </div>
            </div>
        `;

    modal.innerHTML = content;
    document.body.appendChild(modal);

    // 모달 외부 클릭 시 닫기
    modal.addEventListener("click", function (e) {
      if (e.target === this) {
        ModalUtils.closeErrorModal(modalId);
      }
    });
  },

  /**
   * 오류 모달 닫기
   * @param {string} modalId - 모달 ID
   */
  closeErrorModal: function (modalId = "dynamicErrorModal") {
    const modal = document.getElementById(modalId);
    if (modal) {
      document.body.removeChild(modal);
    }
  },
};

/**
 * 유틸리티 기능
 */
const Utils = {
  /**
   * 날짜 포맷팅
   * @param {string} dateString - 날짜 문자열
   * @param {string} locale - 로케일 (기본값: ko-KR)
   * @returns {string} 포맷된 날짜 문자열
   */
  formatDate: function (dateString, locale = "ko-KR") {
    if (!dateString) return "";
    const date = new Date(dateString);
    return date.toLocaleDateString(locale, {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
    });
  },
};
