using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UnivFI.Infrastructure.Helpers
{
    /// <summary>
    /// Dapper 쿼리 로깅을 위한 유틸리티 클래스
    /// </summary>
    public static class SqlQueryLogger
    {
        /// <summary>
        /// SQL 쿼리와 파라미터를 로깅합니다.
        /// </summary>
        /// <typeparam name="T">로거 타입</typeparam>
        /// <param name="logger">로거 인스턴스</param>
        /// <param name="sql">SQL 쿼리문</param>
        /// <param name="parameters">SQL 파라미터</param>
        /// <param name="callerMemberName">호출자 메서드 이름 (자동 입력)</param>
        /// <param name="callerFilePath">호출자 파일 경로 (자동 입력)</param>
        /// <param name="callerLineNumber">호출자 라인 번호 (자동 입력)</param>
        public static void LogQuery<T>(
            ILogger<T> logger,
            string sql,
            object? parameters,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            try
            {
                // 디버그 모드에서만 로깅하도록 설정
                if (!logger.IsEnabled(LogLevel.Debug))
                    return;

                var formattedSql = sql;

                // 파라미터 처리
                if (parameters != null)
                {
                    // DynamicParameters 객체 처리
                    if (parameters is DynamicParameters dynamicParams)
                    {
                        var dynamicParamNames = dynamicParams.ParameterNames;
                        foreach (var paramName in dynamicParamNames)
                        {
                            var paramValue = dynamicParams.Get<object>(paramName);
                            var valueStr = FormatParamValue(paramValue);
                            formattedSql = formattedSql.Replace($"@{paramName}", valueStr);
                        }
                    }
                    // 일반 객체 처리
                    else
                    {
                        var props = parameters.GetType().GetProperties();
                        foreach (var prop in props)
                        {
                            var paramName = $"@{prop.Name}";
                            var paramValue = prop.GetValue(parameters);
                            var valueStr = FormatParamValue(paramValue);
                            formattedSql = formattedSql.Replace(paramName, valueStr);
                        }
                    }
                }

                // 개행 문자 정리 (가독성 향상)
                formattedSql = CleanupSqlForLogging(formattedSql);

                // 호출자 정보 추출
                var callerInfo = GetCallerInfo(callerFilePath, callerMemberName, callerLineNumber);

                // 로그 출력
                logger.LogDebug($"[{callerInfo}] 실행 SQL 쿼리: {formattedSql}");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "SQL 쿼리 로깅 중 오류 발생");
            }
        }

        /// <summary>
        /// 호출자 정보를 가독성 있게 포맷팅합니다.
        /// </summary>
        private static string GetCallerInfo(string filePath, string memberName, int lineNumber)
        {
            // 파일 경로에서 클래스 이름 추출
            string className = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // 클래스명이 Repository로 끝나는 경우 처리
            if (className.EndsWith("Repository"))
            {
                // 호출자의 스택트레이스 분석
                var stackTrace = new StackTrace(true);
                for (int i = 1; i < Math.Min(stackTrace.FrameCount, 10); i++)
                {
                    var frame = stackTrace.GetFrame(i);
                    var method = frame?.GetMethod();

                    if (method != null && method.DeclaringType != null &&
                        method.DeclaringType.Name.EndsWith("Repository"))
                    {
                        className = method.DeclaringType.Name;
                        memberName = method.Name;
                        break;
                    }
                }
            }

            return $"{className}.{memberName}";
        }

        /// <summary>
        /// 파라미터 값을 로깅용 문자열로 변환합니다.
        /// </summary>
        private static string FormatParamValue(object? paramValue)
        {
            if (paramValue == null)
                return "NULL";

            // IEnumerable 형태의 파라미터 처리 (IN 절에 사용되는 경우)
            if (paramValue is IEnumerable<object> collection && !(paramValue is string))
            {
                return $"({string.Join(", ", collection.Select(item => FormatParamValue(item)))})";
            }

            var valueStr = paramValue.ToString();

            // 문자열 파라미터에 작은따옴표 추가
            if (paramValue is string)
                return $"'{valueStr}'";

            // DateTime 형식 처리
            if (paramValue is DateTime dateTime)
                return $"'{dateTime:yyyy-MM-dd HH:mm:ss}'";

            return valueStr ?? "NULL";
        }

        /// <summary>
        /// SQL 쿼리를 로깅하기 위해 가독성 있게 정리합니다.
        /// </summary>
        private static string CleanupSqlForLogging(string sql)
        {
            // 여러 줄의 공백을 하나의 공백으로 치환
            var result = System.Text.RegularExpressions.Regex.Replace(sql, @"\s+", " ");

            // 주요 SQL 키워드 앞에 개행 추가하여 가독성 향상
            var keywords = new[] { "SELECT", "FROM", "WHERE", "AND", "OR", "ORDER BY", "GROUP BY", "HAVING", "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "OUTER JOIN", "OFFSET", "FETCH" };

            foreach (var keyword in keywords)
            {
                // 키워드가 문장 시작이 아닌 경우에만 개행 추가
                result = System.Text.RegularExpressions.Regex.Replace(
                    result,
                    $@"(?<!\w){keyword}\s",
                    m => $"\n{m.Value}");
            }

            return result;
        }

        /// <summary>
        /// 엔티티 기반 쿼리 작업을 로깅합니다.
        /// </summary>
        /// <typeparam name="T">로거 타입</typeparam>
        /// <param name="logger">로거 인스턴스</param>
        /// <param name="operationType">작업 타입 (SELECT, INSERT, UPDATE, DELETE)</param>
        /// <param name="tableName">테이블 이름</param>
        /// <param name="entity">엔티티 객체</param>
        /// <param name="sensitiveProperties">마스킹할 민감한 속성 이름 배열</param>
        /// <param name="callerMemberName">호출자 메서드 이름 (자동 입력)</param>
        /// <param name="callerFilePath">호출자 파일 경로 (자동 입력)</param>
        /// <param name="callerLineNumber">호출자 라인 번호 (자동 입력)</param>
        public static void LogEntityOperation<T>(
            ILogger<T> logger,
            string operationType,
            string tableName,
            object entity,
            string[] sensitiveProperties = null,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (!logger.IsEnabled(LogLevel.Debug) || entity == null)
                return;

            try
            {
                // 로깅을 위해 엔티티 복제 및 민감 정보 마스킹
                var entityCopy = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                    JsonConvert.SerializeObject(entity));

                // 민감 정보 마스킹
                if (entityCopy != null && sensitiveProperties != null && sensitiveProperties.Length > 0)
                {
                    foreach (var prop in sensitiveProperties)
                    {
                        if (entityCopy.ContainsKey(prop))
                        {
                            var value = entityCopy[prop];
                            if (value != null && value is string strValue && !string.IsNullOrEmpty(strValue))
                            {
                                // 문자열 길이만 표시하거나 일부분만 표시
                                entityCopy[prop] = $"***마스킹됨 ({strValue.Length}자)***";
                            }
                        }
                    }
                }

                var entityJson = JsonConvert.SerializeObject(entityCopy, Formatting.None);

                // 호출자 정보 추출
                var callerInfo = GetCallerInfo(callerFilePath, callerMemberName, callerLineNumber);

                logger.LogDebug($"[{callerInfo}] 실행 SQL 쿼리: {operationType} {tableName} 실행 (Entity: {entityJson})");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "엔티티 작업 로깅 중 오류 발생");
            }
        }

        /// <summary>
        /// ID 기반 간단한 쿼리를 로깅합니다.
        /// </summary>
        /// <typeparam name="T">로거 타입</typeparam>
        /// <param name="logger">로거 인스턴스</param>
        /// <param name="operationType">작업 타입 (SELECT, INSERT, UPDATE, DELETE)</param>
        /// <param name="tableName">테이블 이름</param>
        /// <param name="id">ID 값</param>
        /// <param name="callerMemberName">호출자 메서드 이름 (자동 입력)</param>
        /// <param name="callerFilePath">호출자 파일 경로 (자동 입력)</param>
        /// <param name="callerLineNumber">호출자 라인 번호 (자동 입력)</param>
        public static void LogIdOperation<T>(
            ILogger<T> logger,
            string operationType,
            string tableName,
            object id,
            [CallerMemberName] string callerMemberName = "",
            [CallerFilePath] string callerFilePath = "",
            [CallerLineNumber] int callerLineNumber = 0)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            try
            {
                // 호출자 정보 추출
                var callerInfo = GetCallerInfo(callerFilePath, callerMemberName, callerLineNumber);

                logger.LogDebug($"[{callerInfo}] 실행 SQL 쿼리: {operationType} {tableName} WHERE Id = {id}");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ID 작업 로깅 중 오류 발생");
            }
        }

        /// <summary>
        /// 토큰과 같은 민감한 문자열 데이터를 마스킹하여 반환합니다.
        /// </summary>
        /// <param name="sensitiveString">마스킹할 문자열</param>
        /// <param name="prefixLength">보여줄 접두사 길이</param>
        /// <returns>마스킹된 문자열</returns>
        public static string MaskSensitiveString(string sensitiveString, int prefixLength = 4)
        {
            if (string.IsNullOrEmpty(sensitiveString))
                return "NULL";

            var prefix = sensitiveString.Length > prefixLength ?
                sensitiveString.Substring(0, prefixLength) : sensitiveString;

            return $"{prefix}...{sensitiveString.Length}자";
        }
    }
}