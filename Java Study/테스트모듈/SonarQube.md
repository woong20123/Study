## 설치 방법 
### 윈도우 
* JAVA JDK SE 11버전 설치 합니다. 
  * https://www.oracle.com/kr/java/technologies/javase/jdk11-archive-downloads.html
  * JAVA_HOME 경로를 설정합니다. 

* 소나 큐브를 다운로드 합니다. 
  * https://www.sonarqube.org/downloads/
* 다운 받은 파일의 압축을 풉니다. 
  * sonarqube-8.9.7.52159\bin 경로로 이동합니다. 
  * StartSonar.bat를 실행합니다. 
* http://localhost:9000/ 로 접속합니다. 
  * 접속 기본 정보는 admin/admin
* 프로젝트 생성하기 
  * 프로젝트 이름 및 토큰 설정한다음 실행 

### 우분투
```js
// 파일을 다운 받습니다.(LTS)
wget https://binaries.sonarsource.com/Distribution/sonarqube/sonarqube-8.9.7.52159.zip
// 압축을 풀기 위한 작업 
sudo apt-get install unzip
unzip sonarqube-8.9.7.52159.zip
```


## 설정하기 
* 설치된 sonarqube 폴더의 conf파일 sonar.properties 파일을 수정합니다. 

### 데이터 베이스 설정하기
```js
// 데이터 베이스 연결하기
// Example for PostgreSQL
sonar.jdbc.username=sonarqube
sonar.jdbc.password=mypassword
sonar.jdbc.url=jdbc:postgresql//localhost/sonarqube
```

### Elasticsearch 저장소 설정
* 기본 값은 $SONARQUBE-HOME/data에 저장됩니다 
```js
// 다음과 같이 수정 할 수 있습니다. 
sonar.path.data=/var/sonarqube/data
sonar.path.temp=/var/sonarqube/temp
```

### 웹서버 시작 하기
* 기본 포트 값은 9000 이고 context path는 "/" 입니다.
* 아래의 설정값을 수정할 수 있습니다. 
```js
sonar.web.host=192.168.0.1
sonar.web.port=80
sonar.web.context=/sonarqube
```

## 유저 관리
* Security - Users 항목으로 들어가면 유저를 생성할 수 있습니다. 

## 그룹 관리
* Security - Groups 항목으로 들어가면 유저를 생성할 수 있습니다. 

## IntelliJ와 연동
* IntelliJ에서 `SonarLint`를 설치합니다.  
  * SonarLint만 설치해도 서버 없이 정적 분석이 가능합니다. 
* 플러그인을 설치 후에 intellij를 리스타트 합니다. 
* Settings -> Tools -> SonarLint 에서 sonarQube를 연동합니다.

## 실행하기 
* cmd에서 실행해야합니다.
* `PowerShell`에서는 에러가 납니다.
```bat
 mvn sonar:sonar -Dsonar.projectKey=pbn-schedule-manager -Dsonar.host.url=http://172.19.216.152:9000 -Dsonar.login=7f791224bc4446d7276543525a428f3e6e94dadd
```

* 참고 
  * https://daddyprogrammer.org/post/817/sonarqube-analysis-intergrated-intellij/
  * https://confluence.curvc.com/pages/viewpage.action?pageId=6160585


admin / bard!2345 / 7f791224bc4446d7276543525a428f3e6e94dadd
woong20123 / woong20123 / 77f93cea37a8215c692360eb2382b50fa14b76f6

mvn sonar:sonar -Dsonar.projectKey=pbn-schedule-manager -Dsonar.projectName=pbn-schedule-manager -Dsonar.projectVersion=1.1.0-SNAPSHOT -Dsonar.login=77f93cea37a8215c692360eb2382b50fa14b76f6

mvn sonar:sonar -Dsonar.login=77f93cea37a8215c692360eb2382b50fa14b76f6

mvn sonar:sonar -Dsonar.projectKey=BARD_PBN_SERVICE_pbn-contents-api -Dsonar.host.url=http://172.19.216.152:9000 -Dsonar.login=77f93cea37a8215c692360eb2382b50fa14b76f6

bitbucket 토큰 : Njc5NDgxMDEzMTYwOjx3JYPzMo/TxwN7+PY/ihPF2KHy