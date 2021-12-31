## 웹 애플리케이션 개발
* 스프링 부터는 웹 어플리케이션 배포에 최적화되어 있습니다. 
* 내장형 tomcat과 jetty, 언더토우, Netty을 사용한 내장형 HTTP서버를 쉽게 만들 수 있습니다.
* 대부분의 웹 어플리케이션은 spring-boot-starter-web 모듈을 사용하여 빠르게 시작하고 실행합니다. 
* spring-boot-starter-webflux 모듈을 사용하여 반응형 웹 어플리케이션을 빌드하도록 선택할 수 있습니다. 

### Spring Web MVC 프레임 워크 
* 스프링 웹 MVC 프레임워크는 강력한 `모델 뷰 컨트롤러` 웹 프레임 워크입니다.
* Spring MVC는 `@Controller`혹은 `@RestControlle`r 빈을 생성하여 HTTP 요청을 다룰 수 있다. 
* `@RequestMapping` 어노테이션을 사용해서 HTTP에 매칭할 수 있습니다. 
```java
@RestController
@RequestMapping(value="/users")
public class MyRestController {

    @RequestMapping(value="/{user}", method=RequestMethod.GET)
    public User getUser(@PathVariable Long user) {
        // ...
    }

    @RequestMapping(value="/{user}/customers", method=RequestMethod.GET)
    List<Customer> getUserCustomers(@PathVariable Long user) {
        // ...
    }

    @RequestMapping(value="/{user}", method=RequestMethod.DELETE)
    public User deleteUser(@PathVariable Long user) {
        // ...
    }

}
```

#### Spring MVC Auto-configuration
* 스프링 부터는 대부분의 어플리케이션에 동작하는 스프링MVC에 대한 자동 설정을 제공합니다. 
* 자동 설정은 스프링이 기본적으로 제공하는 최상의 기능을 제공합니다.
  * ContentNegotiatingViewResolver와 BeanNameViewResolver 빈을 포함
  * WebJars 지원을 포함하는 정적 자원 제공을 지원
  * Converter, GenericConverter, Formatter 빈을 자동 등록
  * HttpMessageConverters를 지원
  * MessageCodesResolver 자동 등록
  * 정적 index.html 지원
  * ConfigurableWebBinginIntiailizer 빈의 자동 사용
* 이러한 Spring Boot MVC 사용자 정의 만들고 싶다면 @EnableWebMvc 없이 WebMvcConfigurer타입의 @Configuration을 추가할 수 있다.
* RequestMappingHandlerMapping, RequestMappingHandlerAdapter, ExceptionHandlerExceptionResolver의 사용자정의를 하고 싶은 경우
  * WebMvcRegistrations 유형의 빈을 선언하고 이를 사용하여 해당 구성요소의 사용자 정의 인스턴스를 제공할 수 있습니다. 
* Spring MVC을 완전히 제어하려면 @EnableWebMvc가 추가된 @Configuration을 추가하거나 
* @EnableWebMvc의 인터페이스대로 @Configuration이 달린 DelegatingWebMvcConfiguration를 추가할 수 있습니다.

#### HttpMessageConverters

## 참조 사이즈
* https://inseok9068.github.io/springboot/springboot-reference/#26-%EC%9B%B9-%EC%95%A0%ED%94%8C%EB%A6%AC%EC%BC%80%EC%9D%B4%EC%85%98-%EA%B0%9C%EB%B0%9C
* https://docs.spring.io/spring-boot/docs/2.5.8/reference/html/features.html#features.developing-web-applications