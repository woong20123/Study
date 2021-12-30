## Log4j
### 주요 특징
* 속도와 유연성을 고려하여 디자인 되어 있으며 속도에 최적화 되어 있음
* 멀티 스레드 환경에서 사용해도 안전함
* 계층적인 로그 설정과 처리를 지원
* 출력을 파일,콘솔,OutputStream,java.io.Writer,TCP를 사용하는 원격서버, Unix Syslog데몬, JMS 구독자, 윈도우 NT EventLog, email도 가능

### Log4j 구조
#### Logger 
* Log4J의 핵심 클래스로 로깅이 일어나는 부분을 그룹화해서 필요한 로그만 출력하거나 우선순위 부여 
* 실제 로그 기능을 수행하는 역활을 담당합니다.
#### Appender
* 로그의 출력 위치를 지정합니다. 
  * ConsoleAppender : 콘솔에 로그 메시지 출력
  * FileAppender : 파일에 로그 메시지를 출력합니다.
  * RollingFileAppender : 로그의 크기가 지정한 용량 이상이 되면 다른 이름으로 파일 출력
  * DailyRollingFileAppender : 하루 단위로 로그 메시지를 파일에 출력
  * SMTPAppender : 로그메시지를 이메일로 보낸다.
  * NTEventLogAppender : 윈도우의 이벤트 로그 시스템에 기록한다. 
#### Layout
* 로그의 출력 포맷을 지정한다. 
  * %d : 로그의 기록시간을 출력한다. 
  * %p : 로깅의 레벨을 출력한다. 
  * %F : 로깅이 발생한 프로그램의 파일명을 출력한다. 
  * %M : 로깅이 발생한 메소드의 이름을 출력한다.
  * %l : 로깅이 발생한 호출지의 정보를 출력한다.
  * %L : 로깅이 발생한 호출지의 라인수를 출력한다. 
  * %t : 로깅이 발생한 Thread 명을 출력한다.
  * %c : 로깅이 발생한 카테고리명을 출력한다.
  * %C : 로깅이 발생한 클래스명을 출력한다.
  * %m : 로그 메시지를 출력한다.
  * %n : 개행 문자를 출력한다. 
  * %% : %를 출력한다.
  * %r : 어플리케이션이 시작 이후로 로깅이 발생한 시점까지의 시간을 출력한다.
  * %x : 로깅이 발생한 Thread와 관련된 NDC(nested diagnistic context)를 출력한다.
  * %X : 로깅이 발생한 Thread와 관련된 MDC(message diagnistic context)를 출력한다.

## LogBack
* log4j를 기반으로 새롭게 만든 logging 라이브러리
* slf4j를 통해서 logback으로 통합가능

### 구조
* LogBack 설정 방법은 xml과 Groovy로 할 수 있음
#### Logger
* 위의 log4j와 비슷하게 실제 로그를 수행하는 역활
#### appender
* 로그 메시지가 출력될 대상을 결정
#### Encoder
* Appender에 포함되며 사용자가 지정한 형식으로 표현될 로그 메시지를 반환하는 역활을 담당
#### layout
* https://logback.qos.ch/manual/layouts.html

#### Log Patten
* `logger` : 로그가 발생한 패키지 포함 클래스 정보
* `logger{0}` : 클래스를 제외한 클래스이름만 출력
* `logger{length}` : Logger name 축약 가능. ex)logger{35}
* `C{length}` : 로깅 요청을 발행하는 호출자의 클래스 이름 출력
* `contextName` : 로거의 컨텍스트 이름을 출력
* `d{pattern}` : 날짜 정보 출력 
  * %d : 2006-10-20 14:06:49,812
  * %date{ISO8601} : 2006-10-20 14:06:49,812
  * %date{dd MMM yyyy;HH:mm:ss.SSS} : 20 oct. 2006;14:06:49.812
* `-5level` : 로그 레벨, -5는 출력의 고정폭 값
* `ex{depth}` : 로깅 이벤트와 관련된 exception trace를 출력
* `replace(p){r, t}` : p에 대해서 r의 정규식을 검색해서 t로 치환합니다.
  * %replace(%msg){'\s', ''} : 메시지에서 공백을 제거합니다.
  * %replace(%logger %msg){'\.', '/'} : 여러 키워드 포함 할 수 있음
* `prefix(p)` : 패턴 p에 포함된 각 컨버터에 이름을 붙임
  * %d thread=%thread level=%level logger=%logger user=%X{user} %message
  * %d %prefix(%thread %level %logger %X{user}) %message

#### Format modifiers
* Format modifiers를 통해서 각 데이터 필드의 최소 및 최대 너비 및 정렬을 구성할 수 있습니다.
* Format modifiers의 첫번째인 `-` 문자는 왼쪽 정렬 플래그입니다.
* 출력할 최소 문자수를 나타내기 위해서 10진수 상수를 사용합니다. 
* 출력할 최대 문자수를 나타내기 위해서 `.`과 10진수 상수를 사용합니다. 

|Format modifier|왼쪽정렬|최소값|최대값|설명|
|:--|:--|:--|:--|:--|
|%20logger|false|20|none|Logger이름이 20자 미만시 왼쪽에 공백이 생김|
|%-20logger|true|20|none|Logger이름이 20자 미만시 오른쪽에 공백이 생김|
|%.30logger|NA|none|30|Logger이름이 30자 이상시 시작부분부터 자릅니다.|
|%-.30logger|NA|none|30|Logger이름이 30자 이상시 마지막 부분부터 자릅니다.|
|%20.30logger|false|20|30|Logger이름이 20자 미만시 왼쪽에 공백이 생김, 30자 이상시 시작 부터 자름|
|%-20.30logger|false|20|30|Logger이름이 20자 미만시 오른쪽에 공백이 생김, 30자 이상시 시작 부터 자름|

### 변수 선언
* 변수는 구성 파일 자체에서 한번에 하나씩 정의 하거나 외부 속성 파일 또는 외부 리소스에서 전체적으로 로드 할 수 있습니다.
* 역사적인 이유로 변수를 정의하기 위한 XML 요소는 <property>지만 logback 1.0.7이상에서는 <variable>로 바꿔서 사용 할 수 있습니다.
```xml
<configuration>

  <property name="USER_HOME" value="/home/sebastien" />

  <appender name="FILE" class="ch.qos.logback.core.FileAppender">
    <file>${USER_HOME}/myApp.log</file>
    <!-- 생략 --> 
```
* 위와 같이 동일한 결과를 얻기 위해서 System 속성을 사용하는 방법을 보여줍니다.
```java
java -DUSER_HOME="/home/sebastien" MyApp2
```

* 여러 변수가 필요한 경우에는 모든 변수를 포함할 별도의 파일을 만들 수 있습니다. 
```xml
<configuration>
  <property resource="resource1.properties" />
  <!-- 생략 --> 
</configuration>
```
```java
// 클래스 경로 resource1.properties 파일
USER_HOME=/home/sebastien
```

#### 변수의 기본값
* 특정 상황에서 변수가 선언되지 않았거나 값이 null인 경우에 변수의 기본값을 갖도록 할 수 있습니다.
* Bash 쉘에서와 같이 `:-` 연산자를 사용해서 기본값을 지정할 수 있음
* 예를 들어서 aName이라는 변수가 정의되지 않았다면 `"${aName:-golden}`는 golden으로 정의됩니다.

### Logback-access 패턴

|Conversion Word| 효과|
|:--|:--|
|a / remoteIP|Remote IP 주소|
|A / localIP| Local IP 주소|
|b / B/ bytesSend | Response의 내용 길이 |
|h / clientHost | Remote 호스트 정보 |
|H / protocol | 요청 프로토콜 |
|l | 리모트 로그 이름, logback에서는 항상 '-'을 반환 |
|H / protocol | 요청 프로토콜 |
|reqParameter{paramName}|응답의 매개변수|
|i{header} / header{header}| 요청 헤더, 요청에서 해당 헤더를 찾습니다.|

## 참조 사이즈
* https://goddaehee.tistory.com/45
* https://logback.qos.ch/manual/index.html
* https://logback.qos.ch/manual/layouts.html
* https://logback.qos.ch/manual/layouts.html#AccessPatternLayout
  