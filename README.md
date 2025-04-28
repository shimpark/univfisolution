# SSH 구축 및 설명가이드

### 1. SSH 생성하기

```
C:\Users\luckshim\.ssh>ssh-keygen -t rsa -b 4096 -C "luckshim@univ.me" -f id_rsa_gitlab_univ_luckshim
```

- ssh-keygen 를 통해 생성
- -t rsa : RSA 알고리즘을 사용한다.
- -b 4096 : 4096비트로 설정하여 높은 보안 수준을 제공한다.
- -C "luckshim@univ.me" : 키에 대한 주석을 추가 (보통 키 소유자의 이메일 주소를 사용)
- -f id_rsa_gitlab_univ_luckshim: 생성된 키 파일의 이름과 위치를 지정한다.

### 2. .ssh 폴더의 config 파일 수정

- 파일 위치 : `C:\Users\luckshim\\.ssh`
- config 파일 내의 내용 추가
  - Host : 호출 작명 (`추후 git clone 명칭으로 사용됨`)
  - IdentityFile : 파일 위치 경로임

```
Host gitlab.com-univ_luckshim
    HostName gitlab.com
    User git
    IdentityFile ~/.ssh/id_rsa_gitlab_univ_luckshim
```

### 3. gitlab 의 ssh keys 적용하기

1. 접속 : <https://gitlab.com/-/user_settings/ssh_keys>
2. Add an SSH key 버튼 선택한다.
3. key 의 입력 내용은 생성한 공개키 `id_rsa_gitlab_univ_luckshim.pub` 메모장 열어 내용을 붙여넣기 하면 된다.
4. title 은 구분을 위해 `id_rsa_gitlab_univ_luckshim` 을 입력한다.
5. usage type : authentication & signing 선택한다.
6. 날짜를 제거하면 무제한이다.(날짜 지정 시 최대 1년)
7. 최종 저장한다.

### 4. gitlab 소스를 clone 하는 방법

1. 원본 Clone with SSH : `git@gitlab.com:ebiz-univ/univfisolution.git`
   (바로 사용 시, git@gitlab.com: Permission denied (publickey) 에러 발생)
2. 변경 내용 : git@`gitlab.com`:ebiz-univ/univfisolution.git 에서 gitlab.com 을 config 의 `Host 명` 대체
   (`gitlab.com` -> `gitlab.com-univ_luckshim`)
3. clone 하기 : git clone git@`gitlab.com-univ_luckshim`:ebiz-univ/univfisolution.git
