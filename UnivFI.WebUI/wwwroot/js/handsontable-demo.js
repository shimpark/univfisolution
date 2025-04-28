/**
 * Handsontable 메뉴 관리 데모
 */
document.addEventListener("DOMContentLoaded", function () {
  // 컨테이너 요소 가져오기
  const container = document.getElementById("handsontable-container");

  if (!container) {
    console.error("handsontable-container 요소를 찾을 수 없습니다.");
    return;
  }

  // 메뉴 데이터 가져오기
  fetch("/Handsontable/GetMenuData")
    .then((response) => response.json())
    .then((data) => {
      // Handsontable 초기화
      const hot = new Handsontable(container, {
        data: data,
        rowHeaders: true,
        colHeaders: [
          "ID",
          "메뉴키",
          "제목",
          "URL",
          "아이콘",
          "부모ID",
          "순서",
          "레벨",
          "표시여부",
          "생성일",
          "수정일",
        ],
        columns: [
          { data: "id", type: "numeric", readOnly: true },
          { data: "menuKey", type: "text" },
          { data: "title", type: "text" },
          { data: "url", type: "text" },
          { data: "iconClass", type: "text" },
          { data: "parentId", type: "numeric" },
          { data: "menuOrder", type: "numeric" },
          { data: "levels", type: "numeric" },
          { data: "isVisible", type: "checkbox" },
          {
            data: "createdAt",
            type: "date",
            readOnly: true,
            dateFormat: "YYYY-MM-DD",
            correctFormat: true,
          },
          {
            data: "updatedAt",
            type: "date",
            readOnly: true,
            dateFormat: "YYYY-MM-DD",
            correctFormat: true,
          },
        ],
        height: "auto",
        licenseKey: "non-commercial-and-evaluation", // 비상업적 라이센스 키
        stretchH: "all",
        columnSorting: true,
        filters: true,
        dropdownMenu: true,
        multiColumnSorting: true,
        manualColumnResize: true,
        manualRowResize: true,
        contextMenu: true,
        search: true,
        width: "100%",
        autoColumnSize: {
          samplingRatio: 0.3,
        },
        afterGetColHeader: function (col, TH) {
          TH.className = "htCenter htMiddle font-bold bg-gray-100";
        },
      });

      // 검색 UI 추가
      const searchField = document.createElement("div");
      searchField.className = "search-field mt-2 mb-4";
      searchField.innerHTML = `
        <div class="flex items-center justify-end">
          <input type="text" id="search-input" placeholder="검색어 입력..." 
            class="p-2 border border-gray-300 rounded-l focus:outline-none focus:ring-2 focus:ring-blue-500">
          <button id="search-button" 
            class="bg-blue-600 hover:bg-blue-700 text-white font-medium p-2 rounded-r transition-colors">
            <i class="fas fa-search"></i>
          </button>
        </div>
      `;

      container.parentElement.insertBefore(searchField, container);

      // 검색 기능 구현
      const searchInput = document.getElementById("search-input");
      const searchButton = document.getElementById("search-button");

      const search = function () {
        const queryResult = hot.getPlugin("search").query(searchInput.value);
        hot.render();
      };

      searchButton.addEventListener("click", search);
      searchInput.addEventListener("keydown", function (event) {
        if (event.key === "Enter") {
          event.preventDefault();
          search();
        }
      });

      // 윈도우 리사이즈 이벤트에 대응
      window.addEventListener("resize", function () {
        hot.render();
      });
    })
    .catch((error) => {
      console.error("메뉴 데이터를 불러오는 중 오류가 발생했습니다:", error);
      container.innerHTML = `
        <div class="text-center py-8 text-red-600">
          <i class="fas fa-exclamation-triangle text-3xl mb-4"></i>
          <p class="font-semibold">데이터를 불러올 수 없습니다.</p>
          <p>오류: ${error.message || "알 수 없는 오류가 발생했습니다."}</p>
        </div>
      `;
    });
});
