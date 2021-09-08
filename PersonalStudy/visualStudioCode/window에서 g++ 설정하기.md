## 참고 사이트
* https://code.visualstudio.com/docs/cpp/config-mingw

## MSYS2
* MSYS2를 통해서 Mingw-w64의 최신 버전을 설치합니다. 
  * https://www.msys2.org/
* 설치된 MSYS2를 실행합니다.
  * pacman -Syu : 최신버전으로 패키지를 동기화 합니다. 
* pacman -S mingw-w64-x86_64-gcc을 통해서 gcc 설치
  * pacman -S mingw-w64-x86_64-toolchain
* 설치된 폴더를 기본값"C:\msys64\mingw64\bin"을 path에 등록합니다. 
  * 내 컴퓨터 -> 속성 -> 환경 변수 -> path 등록 
* 명령 프롬프트를 실행시키고 "g++ -v"를 실행합니다. 