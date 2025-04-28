using System;
using System.Collections.Generic;
using Dapper.Contrib.Extensions;

namespace UnivFI.Domain.Entities
{
    [Table("FileAttach")]
    public class FileAttachEntity
    {
        /// <summary>
        /// 파일 첨부 ID
        /// </summary>
        [Dapper.Contrib.Extensions.Key]
        public long FileAttachId { get; set; }

        /// <summary>
        /// 파일 경로
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 파일 이름
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// 파일 유형
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// 파일 크기
        /// </summary>
        public long? FileLength { get; set; }

        /// <summary>
        /// 파일 고유 식별자
        /// </summary>
        public string FileGUID { get; set; }

        /// <summary>
        /// 생성 일자 (추가 필드)
        /// </summary>
        public DateTime? CreatedAt { get; set; }
    }
}