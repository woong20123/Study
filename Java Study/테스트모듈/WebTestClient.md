## WebTestClient
* WebTestClient는 WebClient를 감싸고 있으며 WebClient를 사용해서 요청을 수행후 응답을 검증합니다.
  
## Setip
### Bind to Controller
* 다음은 하나의 @Controller를 테스트하기 위한 서버 설정 방법
```java
// Webflux Java configuration을 로드하고 인자를 컨트롤러에 등록 
// WebFlux 어플리케이션은 Mock Request, Response 객체를 사용해서 HTTP 서버 없이 테스트 됩니다. 
client = WebTestClient.bindToController(new TestController).build();
```
### bind to Rounter Function
* 다음은 RouterFunction을 통해서 서버를 설정합니다.
```java
RouterFunction<?> route = ...
client = WetTestClient.bindToRouterFunction(route).build();
```

### bind to ApplicationContext
* 다음은 어플리케이션의 spring configuration을 통해서 서버를 설정합니다.
```java
@SpringJUnitConfig(WebConfig.class) // 로드할 configuration 클래스를 명시합니다.
class MyTests {
    WebTestClient client;

    @BeforeEach
    void setUp(ApplicationContext context) {                                // 해당 설정을 주입합니다. 
        client = webTestClient.bindToApplicationContext(context).build();   // WebTestClient를 생성합니다.
    }
}
```

### Bind to Server
* 서버에 직접 연결합니다.
```java
client = WebTestClient.bindToServer().baseUrl("http://locakhost:8080")
```

### Client builder
* 서버 설정 옵션들과 다양한 옵션들로 설정합니다. 
* 서버에서 클라이언트 설정으로 이행을 위해서 configureClient()를 사용해야 합니다. 
```java
client = WebTestClient.bindToController(new TestController())
    .configureClient()
    .baseUrl("/test")
    .build()
```

## 참조 사이즈
* https://xlffm3.github.io/spring%20&%20spring%20boot/webtestclient/#1-%EC%84%9C%EB%A1%A0