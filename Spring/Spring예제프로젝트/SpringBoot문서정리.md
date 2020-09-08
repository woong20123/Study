# 문서정리
## 시스템 요구 사항
### 빌드툴
|Build Tool|버전|
|:--:|:--:|
|Maven|3.3+|
|Gradle|6.3 이상|

### 서블릿 컨테이너 
서블릿 컨테이너는 아래 목록을 포함하고 있습니다. 
|이름|서블릿 버전|
|:--:|:--:|
|Tomcat 9.0|4.0|
|Jetty|3.1|
|Undertow 2.0|4.0|
Undertow는 Java로 작성된 유연한 성능의 웹서버 입니다. 동기 비동기 둘 다 지원

### POM 생성 
Maven을 사용한다면 pom.xml 파일을 생성해서 의존성관리를 시작하세요


## annotation
## 예제 코드
```java
@RestController
@EnableAutoConfiguration
public class Example {

    @RequestMapping("/")
    String home() {
        return "Hello World!";
    }

    public static void main(String[] args) {
        SpringApplication.run(Example.class, args);
    }

}
```

### @RestController
웹서버로 들어오는 요청을 처리합니다.
예제에서 Example 클래스는 @RestController로 지정되었습니다, 

### @RequestMapping
라우팅 정보를 제공합니다. 
예제에서 home 메서드는 / 경로에 맵핑됩니다. 

### @SpringBootApplication 
하나 이상의 @Bean 메서드를 선언하고 자동구성 및 구성 요소 스캔을 트리거합니다. 
@Configuration, @EnableAutoConfiguration, @ComponentScan을 동시에 선언한 것과 같습니다.

### @JsonIgnoreProperties

