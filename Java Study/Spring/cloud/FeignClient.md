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
* @FeignClient 애노테이션에서 String 값은 Spring Cloud LoadBalancer 클라이언트를 생성하는데 사용되는 임의 클라이언트 이름입니다. 
* 또한 url 속성을 사용해서 URL을 지정할 수 있습니다. (절대값 또는 호스트 이름)
* 애플리케이션 컨텍스트의 bean이름은 인터페이스의 이름입니다. 
* 고유한 별칭 값을 지정하려면 qualifiers 값을 사용할 수 있습니다. 
* 위의 로드 밸런서 클라이언트는 "stores" 서비스에 대한 물리적 주소를 검색하기를 원합니다. 
* 애플리키케이션이 Eureka 클라이언트의 경우 Eureka 서비스 레지스트리에서 서비스를 확인합니다.
* Eureka를 사용하지 않으려면 SimpleDiscoveryClient를 사용하면 external 구성에서 서버 목록을 구성할 수있습니다. 
* Spring Cloud OpenFeign은 Spring Cloud LoadBalancer의 블로킹 모드에 사용 할 수 있는 모든 기능을 지원합니다. 
* 프로젝트 문서에서 이에 대한 자세한 내용을 읽을 수 있습니다. [링크](https://docs.spring.io/spring-cloud-commons/docs/current/reference/html/#spring-cloud-loadbalancer).
### 확인 사항
* @Configuration-annotation-classes 에서 @EnableFeignClients 애너테이션을 사용하려면 클라이언트 위치를 지정 해야 합니다.
* 예제 
  * 리스트 : @EnableFeignClients(basePackages = "com.example.clients")
  * 명시적 나열 : @EnableFeignClients(clients = InventoryServiceFeignClient.class)

## Feign 기본값 Overriding
* Spring Cloud의 Feign 지원의 핵심 개념은 명명된 클라이언트입니다.
* 각 feign 클라이언트는 요청시 원격 서버에 연결하기 위해 함께 작동하는 구성 요소의 일부이며 그 조합은 @FeignClient 애노테이션을 사용해서 애플리케이션 개발자가 부여한 이름입니다.
* Spring Cloud는 FeignClientsConfiguration을 사용하여 각 명명된 클라이언트에 대한 ApplicationContext로 새로 조합을 생성합니다. 
* 여기에는 feign.Decoder, feign.Encoder, feign.Contract 포함됩니다.ㅏ
* @FeignClient 애노테이션의 contextId 속성을 사용해서 해당 조합의 이름을 재 정의할 수 있습니다.
* Spring Clound를 사용하면 @FeignClient를 사용해서 추가적인 구성을 선언해서 Feign 클라이언트를 완벽하게 제어할 수 있습니다.
```java
@FeignClient(name = "stores", configuration = FooConfiguration.class)
public interface StoreClient {
    //..
}
```
* 다음과 같은 경우에는 FeignClientsConfiguration에 있는 구성 요소와 FooConfiguration에 있는 구성 요소로 구성됩니다.


### Reactive Support
* OpenFeign 프로젝트는 현재 Spring WebClient와 같은 반응 클라이언트를 지원하지 않기 때문에 Spring Cloud OpenFeign도 지원하지 않습니다. 
* spring core 프로젝트에서 사용할 수 있게 되는 즉시 여기에 지원을 추가할 것입니다.