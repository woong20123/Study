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

## 참조 사이트
* https://godekdls.github.io/Spring%20REST%20Docs/gettingstarted/
  