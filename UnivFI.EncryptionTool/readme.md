# 사용법

PS E:\gitlab\UnivFIManagement> cd univfi.encryptiontool
PS E:\gitlab\UnivFIManagement\UnivFI.EncryptionTool> dotnet run "K8gT1zX9pQm7vB3eR6yUwJ2sH4dF5lA0cN8bV1xZ2yW3"

===================================

암호화할 서버 주소를 입력하세요:
.

암호화할 데이터베이스명을 입력하세요:
db_lucky

암호화할 사용자 ID를 입력하세요:
shimpark

암호화할 비밀번호를 입력하세요:
eogkrsodlf1!

# 암호화 결과:

=============
암호화된 서버 주소: 9JaiOShrQvZHpwSMKG3OfA==
암호화된 데이터베이스명: JhgJfaxNrEvvCUEO5mUj2w==
암호화된 사용자 ID: lWrfZ/luTiY3HkhPBF7Gog==
암호화된 비밀번호: wSM/ZgC/+q4vrJkFDXYphw==

```
appsettings.json에 다음 구성을 추가하세요:
"ConnectionStrings": {
  "EncryptedServer": "9JaiOShrQvZHpwSMKG3OfA==",
  "EncryptedDatabase": "JhgJfaxNrEvvCUEO5mUj2w==",
  "IntegratedSecurity": "false",
  "EncryptedUserId": "lWrfZ/luTiY3HkhPBF7Gog==",
  "EncryptedPassword": "wSM/ZgC/+q4vrJkFDXYphw=="
}
```
