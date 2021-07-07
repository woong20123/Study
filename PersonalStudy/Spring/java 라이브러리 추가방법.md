## 이클립스에서 jar 파일 추가 방법
* 프로젝트 우클릭 -> Properties -> Java Build Path로 이동
* Library 버튼을 클릭해서 Add JARs 버튼을 통해서 라이브러리 추가
* Order and Export버튼에서 추가한 라이브러리 체크
### Maven 빌드시 추가사항
* pom.xml에 다음과 같이 추가
  * ${project.basedir} 는 프로젝트의 경로를 뜻합니다.
```xml
<dependency>
    <groupId>com.plaync.ncommunity</groupId>
    <artifactId>n-community-edge-clients</artifactId>
    <version>2.3.0</version>
    <scope>system</scope>
    <systemPath>${project.basedir}/src/lib/n-community-edge-reactive-client-2.3.0-20210704.231557-5.jar</systemPath>
</dependency>
```