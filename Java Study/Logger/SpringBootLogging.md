# Spring Log reference
* 스프링 공식 문서 Log 부분 번역

## Logging
* 스프링 boot의 모든 내부 로깅은 Commons Logging을 사용하지만 근본적인 로깅 구현은 남겨놈
  * Java Util Logging, Log4J2, Logback은 기본으로 제공
* Starter를 사용한다면 Logback이 기본으로 사용됩니다. 
* 일반적으로 로그의 의존성을 변경할 필요는 없습니다.

### 로그 포맷
```java
2014-03-05 10:57:51.112 INFO 45469 --- \[ main\] org.apache.catalina.core.StandardEngine : Starting Servlet Engine: Apache Tomcat/7.0.52 
2014-03-05 10:57:51.253 INFO 45469 --- \[ost-startStop-1\] o.a.c.c.C.\[Tomcat\].\[localhost\].\[/\] : Initializing Spring embedded WebApplicationContext 
2014-03-05 10:57:51.253 INFO 45469 --- \[ost-startStop-1\] o.s.web.context.ContextLoader : Root WebApplicationContext: initialization completed in 1358 ms 
2014-03-05 10:57:51.698 INFO 45469 --- \[ost-startStop-1\] o.s.b.c.e.ServletRegistrationBean : Mapping servlet: 'dispatcherServlet' to \[/\] 
2014-03-05 10:57:51.702 INFO 45469 --- \[ost-startStop-1\] o.s.b.c.embedded.FilterRegistrationBean : Mapping filter: 'hiddenHttpMethodFilter' to: \[/\*\]
```
* 시간과 날짜는 밀리 세컨트까지 출력
* 로그 레벨 : ERROR > WARN > INFO > DEBUG > TRACE
  * Logback은 FATAL레벨이 없고 ERROR에 대응 
* 프로세스 아이디
* `---` 실제 로그 메시지 구분
* 스레드 이름
* 소스 클래스 드림 
* 로그 메시지 

### 콘솔 아웃품
* 디폴트 로그 설정은 기본적으로 ERROR, WARN, INFO레벨의 메시지가 로깅됨
* 디버그 모드로 실행 되었을 때 핵심 로거를 선택하면 추가 정보를 보여줌(--trace)
```java
java -jar myapp.jar --debug
```

#### 컬러 코드 아웃풋
* 터미널이 ansi를 지원한다면 spring.output.ansi.enabled를 통해서 설정할 수 있습니다
  * FATAL, ERROR: Red
  * WARN: Yellow
  * INFO, DEBUG, TRACE: Green

### 파일 아웃품
* Spring boot는 로그 파일을 쓰지 않고 콘솔에 로그를 찍습니다. 
* 파일로 쓰고 싶다면 logging.file 또는 logging.path를 사용합니다. 
  * 10MB 도달 시 Rotate하고 ERROR, WARN, INFO를 찍습니다. 
* logging.file.max-size로 사이즈를 설정합니다. 
* logging.file.max-history로 최대 저장 파일 갯수를 지정합니다.

### 로그 레벨
* 환경 설정을 통해서 Logger Level을 설정할 수 있습니다.
```yaml
# logging.level.<logger-name>=<level>

# root는 기본 로그 설정
logging.level.root=WARN
# org.springframework.web은 DEBUG로 출력합니다. 
logging.level.org.springframework.web=DEBUG
logging.level.org.hibernate=ERROR
```

### 로그 그룹
* 관련 있는 로거끼리 묶어서 동시에 설정하게 할 수 있습니다.
  * 기본으로 제공되는 그룹
  * Name: web, Loggers: org.springframework.core.codec, org.springframework.http, org.springframework.web
  * Name: sql, Loggers: org.springframework.jdbc.core, org.hibernate.SQL
```yaml
# 로거 그룹에 tomcat 관련된 내용을 묶습니다.
logging.group.tomcat=org.apache.catalina, org.apache.coyote, org.apache.tomcat
# TRACE로 설정
logging.level.tomcat=TRACE
```

### 커스텀 로그 설정
* 다양한 로깅 시스템은 클래스 패스에 라이브러리를 추가해서 설정 할 수 있습니다. 
* Logback
  * logback-spring.xml, logback-spring.groovy, logback.xml, logback.groovy
* Log4j2
  * log4j2-spring.xml, log4j2.xml
* JDK
  * logging.properties
* 가능하다면 -spirng이 붙은 명칭을 사용하는 것이 좋습니다. 
* 다음과 같은 속성은 시스템 속성으로 전송됩니다. 

|Spring Environment|System Property|Comments|
|:--|:--|:--|
|logging.exception-conversion-word |LOG_EXCEPTION_CONVERSION_WORD|exception을 기록 할 때 사용되는 변환단어|
|logging.file.name|LOG_FILE|정의 된경우 기본 로그 구성에서 사용됨|
|logging.file.path|LOG_PATH|정의 된경우 기본 로그 구성에서 사용됨|
|logging.pattern.console|CONSOLE_LOG_PATTERN|콘솔에서 사용할 로그 패턴|
|logging.pattern.dateformat|LOG_DATEFORMAT_PATTERN|로그날짜 형식에 대한 Appender 패턴입니다.|
|logging.charset.console|CONSOLE_LOG_CHARSET|콘솔에서 사용할 charset |
|logging.pattern.file|FILE_LOG_PATTERN|파일에서 사용할 로그패턴|
|logging.charset.file|FILE_LOG_CHARSET|파일에서 사용할 charset|
|logging.pattern.level|LOG_LEVEL_PATTERN|로그 수준을 렌더링 할 대 사용할 형식|
|PID|PID|현재 프로세스 ID|

* logBack을 사용한다면 사용할 수 있는 환경 변수
  
|Spring Environment|System Property|Comments|
|:--|:--|:--|
logging.logback.rollingpolicy.file-name-pattern|LOGBACK_ROLLINGPOLICY_FILE_NAME_PATTERN|롤오버된 로그파일 이름의 패턴|
|logging.logback.rollingpolicy.clean-history-on-start|LOGBACK_ROLLINGPOLICY_CLEAN_HISTORY_ON_START|재시작시 저장소 로그를 정리여부 결정|
|logging.logback.rollingpolicy.max-file-size|LOGBACK_ROLLINGPOLICY_MAX_FILE_SIZE|최대 로그 사이즈|
|logging.logback.rollingpolicy.total-size-cap|LOGBACK_ROLLINGPOLICY_TOTAL_SIZE_CAP|보관할 로그 백업의 총 크기|
|logging.logback.rollingpolicy.max-history|LOGBACK_ROLLINGPOLICY_MAX_HISTORY|보관할 아카이브 로그 파일의 최대 수|

* spring boot의 logger 별 기본 설정 파일
  * https://github.com/spring-projects/spring-boot/blob/v2.5.8/spring-boot-project/spring-boot/src/main/resources/org/springframework/boot/logging/logback/defaults.xml

### Logback Extensions
* Spring Boot에는 LogBack에 대한 여러 확장이 포함될 수 있습니다. 

### 프로필 별 구성
* <springProfile> 태그를 사용하면 활성 Spring 프로필을 기반으로 구성 섹션을 선택적으로 포함하거나 제외 할 수 있습니다.
```xml
<springProfile name="staging">
    <!-- configuration to be enabled when the "staging" profile is active -->
</springProfile>

<springProfile name="dev | staging">
    <!-- configuration to be enabled when the "dev" or "staging" profiles are active -->
</springProfile>

<springProfile name="!production">
    <!-- configuration to be enabled when the "production" profile is not active -->
</springProfile>
``` 

### 환경 속성
* `<springProperty>` 태그를 사용하면 LogBack내에서 사용할 Spring 환경의 속성을 노출 할 수 있습니다.
* Logback 구성에서 application.properties 파일의 값을 접근하려는 경우 유용하게 사용 할 수 있습니다. 
* Logback의 표준 `<property>` 태그와 유사한 방식으로 작동하지만 직접 값을 지정하는 대신 소스를 지정합니다.
* 로컬 범위가 아닌 다른 위치에 속성을 저장해야 하는 경우 범위 속성을 사용 할 수 있습니다.
* 기본값이 필요한 경우 `defaultValue`속성을 사용 할 수 있습니다.
```xml
<springProperty scope="context" name="fluentHost" source="myapp.fluentd.host"
        defaultValue="localhost"/>
<appender name="FLUENT" class="ch.qos.logback.more.appenders.DataFluentAppender">
    <remoteHost>${fluentHost}</remoteHost>
    ...
</appender>
```



## 참조 사이즈
* https://pjh3749.tistory.com/259
* https://docs.spring.io/spring-boot/docs/2.5.8/reference/html/features.html#features.logging