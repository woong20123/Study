# 문서 정리 
## 특징
Maven은 자바용 프로젝트 관리 도구입니다.<br>
필요한 라이브러리를 pom.xml에 정의 해놓으면 작동하는데 필요한 다른 라이브러리들까지 관리해줍니다. (의존성 관리)
파이썬의 pip 같은 기능을 지원함

## Maven 설정 파일
### Setting.xml
#### 메이븐 빌드 툴과 관련한 설정파일 
#### MAVEN_HOME/conf 디렉토리에 위치
#### repository : 패키지 관리 저장소 위치 지정

### POM(Project Object Model) pom.xml
#### 메이븐을 이용하는 프로젝트 root에 존재합니다. 
#### 이곳에 프로젝트에 관련된 의존성을 설정합니다. 
#### 엘리먼트
modelVersion : POM model의 버전<br>
parent : 프로젝트의 계층정보<br>
groupId : 프로젝트를 생성하는 조직의 고유 아이디를 결정한다. 도메인 이름을 거꾸로 적는다. <br>
artifactId : 프로젝트 빌드시 파일 대표 이름이다. groupId내에서 유일해야 한다.<br> 
version : 프로젝트의 현재 버전, 프로젝트가 개발중일 때는 SNAPSHOT을 접미사로 사용<br>
packaging : 패키징 유형(jar, war, ear등)<br>
name : 프로젝트, 프로젝트 이름 <br>
description : 프로젝트에 대한 간략한 설명<br>
url : 프로젝트에 대한 참고 Reference 사이트 <br>
properties : 버전관리시 변수로 사용가능<br>
```xml
<properties> 
<!-- 버전관리시 용이 하다. ex) 하당 자바 버전을 선언 하고 dependencies에서 다음과 같이 활용 가능 하다. <version>${java.version}</version> --> 
<java.version>1.8</java.version> </properties>

```
dependincies : dependincies 태그에는 프로젝트와 의존 관계에 있는 라이브러리들을 관리한다. 
build : 빌드에 사용할 프러그인 목록