## 링크 참조
* https://docs.spring.io/spring-cloud-openfeign/docs/current/reference/html/

* 이 프로젝트는 자동 구성 및 Spring 환경 및 기타 Spring 프로그래밍 모델 문법을 통해서 SpringBoot 앱을 위한 OpenFeign 통합을 제공합니다. 

## 선언적 REST Client: Feign
* Feign은 선언적 웹 서비스 클라이언트입니다. 웹 서비스 클라이언트를 더 쉽게 작성 할 수 있습니다. 
* Feign을 사용하려면 인터페이스를 만들고 애너테이션을 답니다. 
* Feign 주석 및 JAX-RS 주석을 포함한 플러그형 주석 지원이 있습니다. 
* Feign은 플러그형 인코더와 디코더도 지원합니다. 
* Spring Cloud는 Spring MVC 애노테이션을 지원하고 Spring Web에서 기본적으로 사용되는 동일한 HttpMessageConverters를 사용합니다. 
* Spring Cloud는 Feign을 사용할 때 로드 밸런싱된 Http 클라이언트를 제공하기 위해 Eureka, Spring Cloud CircuitBreaker 및 Spring Cloud LoadBalancer를 통합합니다. 
  
## Feign을 추가하는 방법 
* 프로젝트에 Feign을 포함하려면 그룹이 org.springframework.cloud 이고 spring-cloud-starter-openfeign인 아티펙트 ID인 스타터를 사용합니다.
* 현재 Spring Cloud Release Train으로 빌드 시스템을 설정하는 방법은 페이지를 참조하세요
* spring boot app 예제
```java
@SpringBootApplication 
@EnableFeignClients
public class Application {
    public static void main(String[] args){
        SpringApplication.run(Application.class, args);
    }
}

// StoreClient.java
@FeignClient("stores")
public interface StoreClient {
    @RequestMapping(method = RequestMethod.GET, value = "/stores")
    List<Store> getStores();

    @RequestMapping(method = RequestMethod.GET, value = "/stores")
    List<Store> getStores(Pageable pageable);

    @RequestMapping(method = RequestMethod.POST, value = "/stores/{storeId}", consumes = "application/json")
    Store update(@PathVariable("storeId") Long storeId, Store store);

    @RequestMapping(method = RequestMethod.DELETE, value = "/stores/{storeId:\\d+}")
    void delete(@PathVariable Long storeId);
}
```
* Store