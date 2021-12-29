## Requirements
* 자바8, 스프링 프레임 워크5(5.0.2 이상)

## Build configuration
* 빌드 설정
```xml
<!-- mockMVC용 dependency -->
<dependency> 
	<groupId>org.springframework.restdocs</groupId>
	<artifactId>spring-restdocs-mockmvc</artifactId>
	<version>{project-version}</version>
	<scope>test</scope>
</dependency>
<!-- webTestclient용 dependency -->
<dependency>
    <groupId>org.springframework.restdocs</groupId>
    <artifactId>spring-restdocs-webtestclient</artifactId>
    <version>2.0.5.RELEASE</version>
</dependency>

<!-- ascii doctor 플러그인 추가  -->
<build>
	<plugins>
		<plugin> 
			<groupId>org.asciidoctor</groupId>
			<artifactId>asciidoctor-maven-plugin</artifactId>
			<version>1.5.8</version>
			<executions>
				<execution>
					<id>generate-docs</id>
					<phase>prepare-package</phase> <!-- 문서를 패키지 않에 넣을 수 있음 -->
					<goals>
						<goal>process-asciidoc</goal>
					</goals>
					<configuration>
						<backend>html</backend>
						<doctype>book</doctype>
					</configuration>
				</execution>
			</executions>
			<dependencies>
				<dependency> <!-- .adoc 파일에서 사용할 target/generated-snippets 설정 -->
					<groupId>org.springframework.restdocs</groupId>
					<artifactId>spring-restdocs-asciidoctor</artifactId>
					<version>{project-version}</version>
				</dependency>
			</dependencies>
		</plugin>
        <plugin> <!-- jar 빌드전에 문서를 생성하여 포함 시킵니다.) -->
            <artifactId>maven-resources-plugin</artifactId>
            <version>2.7</version>
            <executions>
                <execution>
                    <id>copy-resources</id>
                    <phase>prepare-package</phase>
                    <goals>
                        <goal>copy-resources</goal>
                    </goals>
                    <configuration> <!-- (3) -->
                        <outputDirectory>
                            ${project.build.outputDirectory}/static/docs
                        </outputDirectory>
                        <resources>
                            <resource>
                                <directory>
                                    ${project.build.directory}/generated-docs
                                </directory>
                            </resource>
                        </resources>
                    </configuration>
                </execution>
            </executions>
        </plugin>
	</plugins>
</build>
```

## packaging the Documentation
* 생성한 문서를 jar파일에 패키징 하고 싶다면 다음과 같은 설정을 합니다
```xml
<plugin> 
	<artifactId>maven-resources-plugin</artifactId>
	<version>2.7</version>
	<executions>
		<execution>
			<id>copy-resources</id>
			<phase>prepare-package</phase>
			<goals>
				<goal>copy-resources</goal>
			</goals>
			<configuration>
				<outputDirectory>
					${project.build.outputDirectory}/static/docs
				</outputDirectory>
				<resources>
					<resource>
						<directory>
							${project.build.directory}/generated-docs
						</directory>
					</resource>
				</resources>
			</configuration>
		</execution>
	</executions>
</plugin>
```

## Documentation Snippets 생성하기 
* 스프링 Rest Docs은 MVC 테스트 프레임워크나 스프링 웹플럭스의 WebTestClient을 사용해서 문서화한 서비스로 요청을 보냄
* 이 요청과 응답의 결과로 문서 스니펫을 만듭니다.

### Setting up Your Tests
#### setting up JUnit 5 Test
* 가장 먼저 테스트 클래스에 RestDocumentationExtension을 적용해야함
* RestDocumentationExtension는 다음과 같이 출력 디렉토리를 자동 설정합니다.
  * Maven : target/generated-snippets
  * Gradle : build/generated-snippets
```java
@ExtendWith(RestDocumentationExtension.class, SpringExtension.class)
public class JUnit5ExampleTest{
    // Junit 5.1을 사용한다면 extension을 테스트 클래스 필드로 등록해
    // 생성 시점에 출력 디렉토리를 제공하면 디폴드 설정의 재정의 가능 
    @RegisterExtension
    final RestDocumentationExtension restDocumentation = new RestDocumentationExtension("custom");

    private WebTestClient webTestClient;

    // WebTestClient를 설정합니다. 
    // import static org.springframework.restdocs.webtestclient.WebTestClientRestDocumentation.documentationConfiguration 가져올 수 있음
    @BeforeEach
    public void setUp(ApplicationContext applicationContext,
            RestDocumentationContextProvider restDocumentation) {
        this.webTestClient = WebTestClient.bindToApplicationContext(applicationContext)
                .configureClient()
                .filter(documentationConfiguration(restDocumentation)) // (1)
                .build();
    }
}
```

### RESTful Service 실행
```java
import static org.springframework.restdocs.webtestclient.WebTestClientRestDocumentation.document
    //....

    // /siege/static api호출이 application/json 응답이 필요하다고 알립니다.
    this.webTestClient.get().uri(uriBuilder -> uriBuilder
                        .path("/siege/static")
                        .queryParam("date", "20210822")
                        .queryParam("server_id", "107").build())
                        .accept(MediaType.APPLICATION_JSON) 
                        .exchange()
                        .expectStatus().isOk()  // 기대한 결과를 응답하는지 검증합니다.
                        // staticData 디렉토리에 스니펫을 작성해 서비스 호출을 문서화함 
                        // 스니펫은 ExchangeResult의 Consumer가 작성합니다. 
                        .expectBody().consumeWith(document("staticData"));  
                        
```

### Snippets을 사용하기 
* 생성된 스니펫을 사용하려면 .adoc 소스 파일을 만들어야 합니다. 
* .adoc으로 끝나기만 하면 어떤 이름이든 상관 없음
* 만들어지는 html파일도 같은 이름으로 생성됨
* 빌드 툴에 따른 경로
  * Maven
    * Source files : src/main/asciidoc/*.adoc
    * Generated files : target/generated-docs/*.html
  * Gradle
    * Source files : src/docs/asciidoc/*.adoc
    * Generated files : build/asciidoc/html5/*.html


# Working with Asciidoctor
* Spring rest docs 관련해서 필요한 Asciidoctor를 다루는 방법을 설명합니다. 

## Snippets 추가하기 
* asciidoc snippets을 추가하는방법을 다룹니다.  
### 다중 Snippets을 포함 Operation
* operation 매크로를 사용하면 특정 연산에 대해 만들어진 스니펫 전체 혹은 일부를 import할 수 있음
* 프로젝트 빌드 설정에 spring-restdocs-asciidoctor을 추가하면 됩니다. 
* 다음과 같은 명령어는 특정 연산에 해당하는 모든 스니펫을 추가합니다. 
```java
// 모든 스니펫을 추가하니다.
operation::index[]

// 일부 스니펫을 추가합니다.
operation::index[snippets='curl-request,http-request,https-response']

// 위의 예제는 다음와 같습니다.
[[example_curl_request]]
== Curl request

include::{snippets}/index/curl-request.adoc[]

[[example_http_request]]
== HTTP request

include::{snippets}/index/http-request.adoc[]

[[example_http_response]]
== HTTP request

include::{snippets}/index/http-response.adoc[]
```

### Section Title
* operation 매크로에 추가 할 수 있는 내장 스니펫
* curl-requst, http-request, http-response, httpie-request, links, request-body, request-fields, response-body, response-fields
* 기본 스니펫을 제외한 스니펫은 `-`문자를 공백으로 첫글자로 대문자로 변경한 제목이 디폴트 입니다.
  * custom-snippet -> Custom snippet
```java
// curl-request를 Example request로 변경한 예제
:operation-curl-request-title: Example request
```

# configuration
## Docuemnted URIs
### MockMvc URI Customization
* MockMvc를 사용하면 스프링 Rest Docsd은 다음을 디폴트 설정을 가집니다. 
* Schema : `http`
* Host : `localhost`
* Port : `8080`
* 설정 값을 셋팅하는 예제 입니다.
```java
this.mockMvc = MockMvcBuilders.webAppContextSetup(this.context)
                .apply(documentationConfiguration(this.restDocumentation).uris()
                                .withSchema("https")
                                .withHost("example.com")
                                .withPort(443))
                .build();
```

### WebTestClient URI Customization
* WebTestClient는 스프링 Rest Docs는 `http://localhost:8080`를 기본 베이스로 작성합니다. 
* URI 베이스는 WebTestClient.Builder에 있는 baseUrl 메서로 커스텀 할 수 있음
```java
public void setUp(){
    this.webTestClient = WebTestClient.bindToApplicationContext(this.context)
            .configureClient()
            .baseUrl("https://api.example.com")
            .filter(documentationConfiguration(this.restDocumentation)
                    .snippets().withEncoding("ISO-8859-1") // 인코딩 추가 작업 
            ).build();
}
```

### Snippet Template Format 변경
* 디폴트 스니펫 템플릿은 asciidoctor이지만 markdown으로 지원함
```java
this.webTestClient = WebTestClient.bindToApplicationContext(this.context).configureClient()
	.filter(documentationConfiguration(this.restDocumentation)
		.snippets().withTemplateFormat(TemplateFormats.markdown()))
	.build();
```

### 기본 Opearation PreProcessors 등록 가능
* 기본 요청/응답 전처리기도 RestDocumentationConfigurer API로 등록할 수 있음
```java
// 다음 예제는 모든 요청에서 Foo 헤더를 제거하고, 모든 응답을 보기 좋게 출력합니다. 
this.webTestClient = WebTestClient.bindToApplicationContext(this.context)
        .configureClient()
        .filter(documentationConfiguration(this.restDocumentation))
            .operationPreprocessors()
                    .withRequestDefaults(removeHeader("Foo"))
                    .withResponseDefaults(prettypPrint()))
        .build();
```

# Documenting API
## Hypermedia
* 하이퍼 기반의 API에서 링크도 문서화 가능 
```java
this.webTestClient().get().uri("/").accept(MediaType.APPICATION_JSON).exchange()
    .expectStatus().isOk().expectBody()
    .consumeWith(document("index", links(       // 응답에 링크가 있는지 검증합니다. 
        linkWithRel("alpha").description("Link to the alpha resource"), // 링크를 설명합니다.
        linkWithRel("bravo").description("Link to the bravo resource"),
    )))
```

## 요청 및 응답 Payloads
* 일반적인 요청/응답 페이로드 문서도 작성 할 수 있습니다.
* 기본적으로 Spring Rest Docs은 요청과 응답 바디를 위한 스니펫을 자동으로 만들어 줍니다. 
* 각 스니펫이름은 `request-body.adoc`과 `response-body/adoc`입니다. 
### 요청 및 응답 필드 
* 아래 페이로드를 필드로 문서화 할 수 있습니다. 
* 필드를 문서화 할 떄는 모든 필드를 작성하지 않는다면 실패합니다. 
* 문서화한 필드가 페이로드에 없을 경우 해당 필드를 선택사항으로 마킹하지 않는다면 테스트는 실패
```json
{
    "contact" : {
        "name" : "Jane Doe",
        "email" : "jane.doe@example.com"
    } 
}
```
```java
this.webTestClient.get().uri("user/5").accept(MediaType.APPLICATION_JSON)
	.exchange().expectStatus().isOk().expectBody()
    .consumeWith(document("user",
        responseFields(
            fieldWithPath("contact.emain").description("The user's email address")
            fieldWithPath("contact.name").description("The user's name")
        )));
```
* 문서의 모든 필드를 상세하게 적고 싶지 않다면 하위 패스를 하나로 묶어서 문서화도 가능합니다. 
```java
this.webTestClient.get().uri("user/5").accept(MediaType.APPLICATION_JSON)
	.exchange().expectStatus().isOk().expectBody()
    .consumeWith(document("user",
        responseFields(
            subsectionWithPath("contact").description("The user's contact details"))));
```
* 만약 모든 필드를 문서화 하지 않아도 테스트가 실패하지 않도록 설정하려면 다음 메소드를 사용하면 된다.
  * org.springframework.restdocs.payload.PayloadDocumentation의 relaxedRequestFields, relaxedResponseFields를 사용

#### JSON 페이로드 필드를 다루는 법
* Spring Rest Docs은 기본적으로 페이로드가 JSON이라고 가정합니다. 
* JSON 필드의 패스는 점이나 괄호 표기법을 사용
  * a.b
  * ['a']['b']
  * a['b']
  
* 예제 JSON 페이로드
```json
{
	"a":{
		"b":[
			{
				"c":"one"
			},
			{
				"c":"two"
			},
			{
				"d":"three"
			}
		],
		"e.dot" : "four"
	}
}
```
* 패스 정보 
  * a : b를 가지고 있는 객체
  * a.b : 객체 세개가 들어있는 배열
  * ['a']['b'] : 객체 세개가 들어있는 배열
  * a.b[] : 객체 세개가 들어있는 배열
  * a.b[].c : 문자열 "one", "two"가 들어있는 배열
  * a.['e.dot'] : 문자열 "four"

* 루트가 배열인 경우 [] 패스가 전체 배열을 가르킴 
* 이름이 다른 필드를 한번에 매칭하고 싶다면 `*`를 사용할 수 있습니다.
```json
// user.*.role -> 문서화 할 수 있음
{
	"users":{
		"ab12cd34":{
			"role": "Administrator"
		},
		"12ab34cd":{
			"role": "Guest"
		}
	}
}
```

#### JSON 필드 타입 
* array : 필드에 사용한 값이 배열일 때
* boolean : 필드의 사용한 값이 boolean
* object : 필드 사용 값이 모두 객체 일 때
* number : 필드에 사용한 값이 모두 숫자일 때
* null : 필드의 사용한 값이 모두 null 일 때
* string : 필드에 사용한 값이 모두 문자열일 때
* varies : 페이로드 내에서 필드를 각기 다른 타입으로 

* 필드타입을 적용한 예제
```java
.consumeWith(document("user",
    responseFields(
        fieldWithPath("contract.email")
            .type(JsonFieldType.STRING)
            .description("The user's email address")
    )));
```

### 페이로드가 크거나 복잡하다면 섹션별로 문서화 할 수 있음

## 요청 파라미터
* request 파라미터도 문서화 할 수 있습니다. 
```java
// WebTestClient get을 통한 파라미터 처리
this.webTestClient.get().uri("/users?page=2&per_page=100") // (1)
	.exchange().expectStatus().isOk().expectBody()
    // requestParameter를 사용해서 문서화 합니다. 
	.consumeWith(document("users", requestParameter(  
        parameterWithName("page").description("The page to retrieve"),
        parameterWithName("per_page").description("Entries per page")

    )));

// WebTestClient post를 통한 파라미터 처리
MultiValueMap<String, String> formData = new LinkedMultiValueMap<>();
formData.add("username", "Tester");
this.webTestClient.post().uri("/users").body(BodyInserters.fromFormData(formData))
        .exchange().expectStatus().isCreated().expectBody()
        .consumeWith(document("create-user", requestParameters(
            parameterWithName("username").description("The user's username")
        )));
```

### Path 파라미터
```java
this.webTestClient.get().uri("/locations/{latitude}/{longitude}", 51.5072, 0.1275) // (1)
	.exchange().expectStatus().isOk().expectBody()
	.consumeWith(document("locations",
		pathParameters( // (2)
			parameterWithName("latitude").description("The location's latitude"), // (3)
			parameterWithName("longitude").description("The location's longitude")))); // (4)
```

### 요청 Part
```java
MultiValueMap<String, Object> multipartData = new LinkedMultiValueMap<>();
multipartData.add("file", "example".getBytes());
this.webTestClient.post().uri("/upload").body(BodyInserters.fromMultipartData(multipartData)) // (1)
	.exchange().expectStatus().isOk().expectBody()
	.consumeWith(document("upload", requestParts( // (2)
		partWithName("file").description("The file to upload")) // (3)
));
```

## HTTP Header 
* 요청, 응답 헤더는 requestHeaders, responseHeader로 문서화 할 수 있음
* `request-headers.adoc`, `request-headers.adoc` 이 만들어집니다. 
```java
this.webTestClient.get().uri("/people").header("Authorization", "Basic fjatjeot09klfv=")
    .exchange().expectStatus().isOk().expectBody()
    .consumeWith(document("headers"),
        requestHeaders(
            headerWithName("Authorization").description("Basic auth credentials")),
        responseHaders(
            headerWithName("X-RateLimit-Limit")
				.description("The total number of requests permitted per period"),
			headerWithName("X-RateLimit-Remaining")
				.description("Remaining requests permitted in current period")
        )
    
    )))
```

## 재사용 Snippets
* 여러 리소스들은 공통 기능이 있습니다. 
* 공통 요소의 Snippet을 재사용하면 중복 코드를 피할 수 있습니다. 
```java
protected final LinksSnippet pagingLinks = links(
		linkWithRel("first").optional().description("The first page of results"),
		linkWithRel("last").optional().description("The last page of results"),
		linkWithRel("next").optional().description("The next page of results"),
		linkWithRel("prev").optional().description("The previous page of results"));

this.webTestClient.get().uri("/").accept(MediaType.APPLICATION_JSON).exchange()
	.expectStatus().isOk().expectBody()
	.consumeWith(document("example", this.pagingLinks.and( // (1)
		linkWithRel("alpha").description("Link to the alpha resource"),
		linkWithRel("bravo").description("Link to the bravo resource"))));

```

## 기본 Snippets 
* 기본으로 생성되는 Snippets 목록
  * curl-request.adoc : 동일한 curl 명령어가 있는 경우
  * httpie-request.adoc : 동일한 HTTPie 명령어가 있는 경우
  * http-request.adoc : HTTP 요청이 있는 경우 
  * http-response.adoc : 반환된 HTTP 응답이 있는 경우
  * request-body.adoc : 전송한 요청 Body가 있는 경우
  * response-body.adoc : 반환된 응답 Body가 있는 경우  
 
## 참조 사이트
* https://godekdls.github.io/Spring%20REST%20Docs/gettingstarted/
  