## JAVA jdk 설치하기
* https://github.com/ojdkbuild/ojdkbuild
* 원하는 버전 설치하기 

## 환경변수 설정
### 윈도우
* 제어판 -> 시스템 및 보안 -> 시스템 -> 고급 시스템 설정 -> 고급 -> 환경 변수 -> 사용자 변수
* 변수 추가
  * JAVA_HOME : JDK 설치 경로 ex) C:\Program Files\AdoptOpenJDK\jdk-11.0.10.9-hotspot
  * CLASSPATH : %CLASSPATH%
  * path : java 실행 경로 추가 ex) C:\Program Files\AdoptOpenJDK\jdk-11.0.10.9-hotspot\bin

## VS CODE 확장자 설치
* Spring Boot Support
* Spring Initializr Java Support
* Spring Boot Extension Pack
* Java Extension Pack
* VS CODE 마켓 플레이스 단축키 -> Ctrl + Shift + X

## Git Clone
* git 설치 : https://git-scm.com/downloads
* VS Code : Ctrl + Shift + P -> Git:Clone
  * https://github.com/spring-guides/gs-spring-boot.git 입력 후 설정할 경로 지정

## Maven 설정
* Maven Project 생성 및 Run
* Maven으로 실행 할 때는 해당 위치로 이동해서 수행


### JAVA 프로그램 만들기
* Ctrl + shift + P -> Java: Create Java Project

### Maven 프로그램 설치 
* https://maven.apache.org/download.cgi 에서 bin.zip 파일 다운로드
  * 적절한 위치에 저장
* VS code의 maven.executable.path : maven을 지정