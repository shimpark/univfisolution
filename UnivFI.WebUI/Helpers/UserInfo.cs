using System.Collections.Generic;

namespace UnivFI.WebUI.Helpers
{
    /// <summary>
    /// JWT 토큰에서 추출한 사용자 정보를 담는 클래스
    /// </summary>
    public class UserInfo
    {
        /// <summary>
        /// 사용자 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 사용자명 (로그인 ID)
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 사용자 실명 또는 표시명
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 이메일 주소
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 사용자의 역할 목록
        /// </summary>
        public List<string> Roles { get; set; } = new List<string>();
    }
}