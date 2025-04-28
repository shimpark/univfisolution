using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using UnivFI.Domain.Entities;
using UnivFI.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;

namespace UnivFI.WebUI.Helpers
{
    /// <summary>
    /// 메뉴 관련 헬퍼 기능을 제공하는 클래스
    /// </summary>
    public static class MenuHelper
    {
        /// <summary>
        /// 사용자의 메뉴 정보를 가져옵니다.
        /// </summary>
        /// <param name="user">현재 사용자</param>
        /// <param name="httpContextAccessor">HTTP 컨텍스트 접근자</param>
        /// <param name="menuRepository">메뉴 리포지토리</param>
        /// <returns>사용자에게 할당된 메뉴 목록</returns>
        public static List<MenuEntity> GetUserMenus(
            ClaimsPrincipal user,
            IHttpContextAccessor httpContextAccessor,
            IMenuRepository menuRepository)
        {
            // 로그인한 사용자 확인
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userId, out int userIdInt);

            // 사용자의 메뉴 목록 가져오기
            var menus = new List<MenuEntity>();

            // 캐싱 키 생성
            string cacheKey = $"UserMenus_{userIdInt}";

            if (userIdInt > 0)
            {
                // 캐시된 메뉴가 있는지 확인
                if (httpContextAccessor.HttpContext.Items.TryGetValue(cacheKey, out var cachedMenus))
                {
                    menus = (List<MenuEntity>)cachedMenus;
                }
                else
                {
                    // 캐시된 메뉴가 없으면 DB에서 조회
                    menus = menuRepository.GetMenusByUserIdAsync(userIdInt).GetAwaiter().GetResult().ToList();
                    // 현재 요청 기간 동안 메뉴 목록 캐싱
                    httpContextAccessor.HttpContext.Items[cacheKey] = menus;
                }
            }

            return menus;
        }

        /// <summary>
        /// 메뉴 계층 구조를 생성합니다.
        /// </summary>
        /// <param name="menus">메뉴 엔티티 목록</param>
        /// <returns>메뉴 계층 구조 배열</returns>
        public static object[] BuildMenuStructure(List<MenuEntity> menus)
        {
            // 상위 메뉴 항목만 필터링 (ParentId가 null인 항목)
            var parentMenus = menus.Where(m => m.ParentId == null).ToList();

            // 메뉴 계층 구조를 구성
            var menuStructure = parentMenus.Select(parentMenu => new
            {
                id = parentMenu.Id,
                title = parentMenu.Title,
                icon = "bi-folder", // 상위 메뉴용 아이콘
                url = parentMenu.Url,
                submenus = menus.Where(m => m.ParentId == parentMenu.Id)
                .Select(childMenu => new
                {
                    id = childMenu.Id,
                    parentId = childMenu.ParentId,
                    title = childMenu.Title,
                    url = childMenu.Url,
                    // URL에서 컨트롤러 추출
                    controller = childMenu.Url?.Split('/')?.Skip(1)?.FirstOrDefault() ?? ""
                }).ToArray()
            }).ToArray();

            return menuStructure;
        }
    }
}