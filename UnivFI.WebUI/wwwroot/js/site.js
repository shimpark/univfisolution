// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 사이트 공통 기능 스크립트
$(document).ready(function () {
  // 이 파일에는 auth-utils.js에 없는 사이트 전반적인 기능만 구현
  // 인증 관련 기능은 auth-utils.js로 이동됨

  // 몇 가지 유용한 간편 함수들
  setupUtilityFunctions();

  // DataTables 초기화 (있는 경우)
  initializeDataTables();
});

// 전역 유틸리티 함수 설정
function setupUtilityFunctions() {
  // 폼 데이터를 JSON으로 변환하는 헬퍼 함수
  $.fn.serializeToJSON = function () {
    var data = {};
    var formData = this.serializeArray();

    $.each(formData, function () {
      if (data[this.name]) {
        if (!Array.isArray(data[this.name])) {
          data[this.name] = [data[this.name]];
        }
        data[this.name].push(this.value || "");
      } else {
        data[this.name] = this.value || "";
      }
    });

    return data;
  };

  // 날짜 포맷팅 함수
  window.formatDate = function (dateString) {
    if (!dateString) return "";

    var date = new Date(dateString);
    return date.toLocaleDateString("ko-KR", {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  // 문자열 길이 제한 함수
  window.truncateString = function (str, maxLength) {
    if (!str) return "";
    if (str.length <= maxLength) return str;
    return str.substr(0, maxLength) + "...";
  };
}

// DataTables 초기화
function initializeDataTables() {
  if ($.fn.DataTable) {
    $(".data-table").each(function () {
      $(this).DataTable({
        language: {
          emptyTable: "데이터가 없습니다",
          info: "_START_ - _END_ / _TOTAL_",
          infoEmpty: "0 개",
          infoFiltered: "(전체 _MAX_ 개 중 검색결과)",
          lengthMenu: "_MENU_ 개씩 보기",
          loadingRecords: "로딩중...",
          processing: "처리중...",
          search: "검색:",
          zeroRecords: "검색된 데이터가 없습니다",
          paginate: {
            first: "처음",
            last: "마지막",
            next: "다음",
            previous: "이전",
          },
        },
        responsive: true,
      });
    });
  }
}
