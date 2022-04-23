## 번역링크 
* https://docs.spring.io/spring-data/mongodb/docs/3.2.10/reference/html/

## 머리말
* Spring Data MongoDB 프로젝트는 핵심 Spring 개념을 MongoDB Document 스타일의 데이터 저장소를 사용하는 개발 솔루션을 적용합니다. 
* document를 저장하고 쿼리하기 위한 상위 수준 추상화 `template`을 제공합니다. 
* Spring Framework에서 제공하는 JDBC 지원과 유사한 점을 알 수 있습니다. 

## 스프링 학습
* Spring Data는 다음을 포함하는 스프링 프레임워크의 핵심 기능 사용합니다.
  * IoC Container
  * type conversion system
  * expression language
  * JMX integration
  * DAO exception hierarchy.
* Spring API를 알필요는 없지만 이면의 개념을 이해하는 것은 중요합니다.
* 최소한 IoC(Inversion of Control)의 이면에 있는 아이디어는 익숙해야 하며 사용하기로 한 IoC 컨테이너는 잘 알고 있어야 합니다. 
* 스프링 컨테이너의 IoC 서비스 지원 없이 MongoDB의 핵심 기능들을 바로 사용 할 수 있습니다. 
* 이것은 Spring컨테이너의 다른 서비스의 지원 없이 바로 사용 할 수 있는 JdbcTemplate와 매우 유사합니다. 
* 저장소 지원과 같은 Spring Data MongoDB의 모든 기능을 활용하려면 Spring을 사용하도록 라이브러리를 일부 구성해야합니다. 

## NoSQL 및 Document 데이터 베이스 학습
* NoSQL 저장소들은 스토리지 세계를 습격하였습니다. 수많은 해결책, 용어 및 패턴이 있는 거대한 영역입니다. (추가로 용어 자체에도 여러 의미가 있습니다.)
* 일부 원칙은 공통적이지만 MongoDB에 어느정도 익숙 해야 합니다.
* 익숙해지는 가장 좋은 방법은 설명서를 읽고 예제를 따르는 것입니다.
* 일반적으로 5-10분 이상 걸리지 않으며 특히 RDBMS의 경험만 있다면 이것은 당신의 눈을 뜨게 해줄 것입니다. 
* MongoDB에 대한 학습의 시작점은 www.mongodb.org입니다. 다음은 유용한 리소스 목록입니다. 
  * The manual introduces MongoDB

### 요구사항
* Spring Data MongoDB 3.x 바이너리는 JDK 8.0이상 및 Spring Framework 5.3.17 이상이 필요합니다.
* document 저장소의 경우 최신 버전을 권장하지만 최소 3.6 버전의 MongoDB가 필요합니다. 

### 호환성 목록
* 다음 호환성 목록은 Spring Dat 버전을 MongoDB driver/database 버전으로 요약합니다. 
* 데이터 베이스 버전은 Spring Data 테스트 스위트를 통과하는 가장 높은 지원 서버 버전을 보여줍니다.
* MongoDB 서버의 변경사항에 영향을 받지 않은 기능을 사용하지 않는 한 최신 서버의 버전을 사용할 수 있습니다.
|Spring Data Release Train|Spring Data MongoDB|Driver Version|Server Version|
|:--|:--|:--|:--|
|2021.0|3.2.x|4.1.x|4.4.x|
|2020.0|3.1.x|4.1.x|4.4.x|

### MongoDB 4.4의 관련 변경 사항
* $text criteria 없을 때 필드 목록에는 텍스트 검색 점수 속성이 포함되지 않아야 합니다. 
* map reduce를 실행 할 때 sort는 빈 document가 아니어야 합니다. 

### MongoDB 4.2 관련 변경 사항
* geoNear 명령 제거
* eval 명령 제거

## 추가 도움말 리소스
* 생략

## Following Development
* 생략

## 뉴스 & 주목할점
### Spring Data MongoDB 3.2의 새로운 기능
* 중첩된 개체를 상위 문서에 Unwrapping을 지원합니다.
  * [링크](https://docs.spring.io/spring-data/mongodb/docs/3.2.10/reference/html/#unwrapped-entities)
* [Support expressions to define field projections.](https://docs.spring.io/spring-data/mongodb/docs/3.2.10/reference/html/#mongo-template.querying.field-selection)
### 나머지 생략

## Upgrading from 2.x to 3.x
* 생략
#### UUID Types
* 이제 MongoDB UUID 표현을 다른 형식으로 구성 할 수 있습니다. 
* MongoClientSettings을 통해서 수행 해야 합니다.
```java
// 예제 UUid Codec Configuration
@Configuration 
public class Config extends AbstaractMongoConfiguration {
  @Override
  public void configureClientSettings(MongoClientSettings.Builder builder){
    builder.uuidRepresentation(UuidRepresentation.STATNDARD);
  }
}
```
#### ReactiveMongoDataBaseFactory에서 지연된 MongoDataBase 조회
* ReactiveMongoDataBaseFactory는 MongoDatabase 대신에 Mono<MongoDatabase>를 반환해서 context-specific routing 기능을 활성화 하기 위해서 Reactor Subsriber 컨텍스트에 대한 접근을 허용합니다. 
* 이 변경 사항은 ReactiveMongoTemplate.getMongoDataBase 과 ReactiveMongoTemplate.getCollection()에 영향을 미치므로 모두 지연 검색을 따라야 합니다.

## Dependencies
* 개별 Spring Data 모듈의 시작 날짜가 다르기 때문에 대부분은 서로 다른 메이저 및 마이너 버전 번호를 가지고 있습니다. 
* 생략

## Spring Data Repositories와 작업하기 
* Spring Data 저장소 추상화의 목표는 다양한 지속성 저장소에 대한 데이터 엑세스 계층을 구현하는 필요한 코드의 양을 줄이는 것입니다. 
### 핵심 개념
* Spring Data 저장소 추상화의 중심은 인터페이스는 `Repository` 입니다. 
* 관리할 도메인 클래스와 도메인 클래스의 ID유형을 인수로 사용합니다.
* 이 인터페이스는 주로 작업할 유형을 캡처하고 이 인터페이스를 확장하는 인터페이스를 찾는 데 도움되는 마커 인터페이스 역활을 합니다. 
* CrudRepository 인터페이스는 관리되는 엔티티 클래스에 대한 상세한 CRUD 기능을 제공합니다. 
```java
// CrudRepository 인터페이스 
public interface CrudRepository<T, ID> extends Repository<T, ID> {
  // 주어진 entity를 저장 합니다. 
  <S extends T> S save(S entity);
  // 주어진 키로 Entity를 검색합니다. 
  Optional<T> findById(ID primaryKey);
  // 전체의 엔티티를 가져옵니다.
  Iterable<T> findAll();
  // 엔티티들의 갯수를 리턴합니다.
  long count();
  // 주어진 entity를 삭제합니다.
  void delete(T entity);
  // ID가 존재하는지 확인 합니다.
  boolean existsById(ID primaryKey);
}
```
* PagingAndSortingRepository 추상화는 엔티티에 대한 페이징 처리를 쉽게 하기 위해서 메서드를 추가합니다.
```java
// 페이징 처리 및 정렬이 쉬운 Repository
public interface PagingAndSortingRepository<T, ID> extends CrudRepository<T, ID> {
  // 정렬 처리
  Iterable<T> findAll(Sort sort);
  // 페이징 처리
  Page<T> findAll(Pageable pageable);
}
```
* 파생으로 구현된 Repository 
```java
interface UserRepository extends CrudRepository<User, Long> {

  long countByLastname(String lastname);
}

interface UserRepository extends CrudRepository<User, Long> {

  long deleteByLastname(String lastname);

  List<User> removeByLastname(String lastname);
}
```

## Introduction
### 문서 구조
* Spring Data MongoDB에서 제공하는 핵심 기능에 대해서 설명하니다. 
* `MongoDB support`는 MongoDB 모듈 기능들에 대해서 소개합니다. 
* `MongoDB Repositories` MongoDB를 위한 repository 지원에 대해서 소개합니다.

## MongoDB support
* MongoDB 지원에는 다양한 기능들이 포함되어 있습니다
  * Mongo드라이버 인스턴스 및 Replica set을 위한 자바 기반의 @Configuration 클래스 또는 XML 네임스페이스로 Spring 구성 지원
  * MongoTemplate Helper 클래스는 일반적인 Mongo 작업을 수행할 떄 생산성을 증가 시킵니다. Document들과 POJOs 사이의 통합 개체 매핑을 포함합니다.
  * Spring에서 가지고 있는 Data Access Exception 계층으로 Exception변환
  * Spring의 Conversion Service와 통합된 기능이 풍부한 객체 매팅
  * 다른 메타 데이터 형식을 지원하기 위한 확장 가능한 annotation 기반 매핑 메타 데이터 입니다.
  * 지속성 및 매칭 라이프 사이클 이벤트 
  * Java 기반 쿼리, Criteria 및 Update DSLs
  * 맞춤형 finder 메서드 지원을 포함한 Repository 인터페이스의 자동 구현 
  * 타입 아전 쿼리들을 지원하는 QueryDSL 통합
  * GeoSpatial 통합
* 대부분의 작업에서는 다양한 매핑 기능을 제공하는 MongoTemplate 또는 Repository 지원을 사용해야 합니다. 
* MongoTemplate은 카운터 증가 또는 특별한 목적의 CRUD 작업과 같은 엑세스 기능을 제공합니다.
* MongoTemplate은 콜백 메서드를 제공하므로 com.mongodb.client.MongoDatabase와 같은 저수준 API 아티펙트를 쉽게 얻을 수 있어 MongoD와 직접 통신 할 수 있습니다. 
* 다양한 API 에 대한 명명 규칙의 목표는 기본 MongoDB Java 드라이버에서 이를 복사하여 기존 지식을 Spring API에 쉽게 매칭할 수 있도록 합니다. 

### 시작하기
* 작업 환경을 설정하는 쉬운 방법은 STS에서 Spring 기반 프로젝트를 만드는 것입니다. 
* 먼저 MongoDB 서버를 설정 해야 합니다.
  * MongoDB를 시작하는 방법 다음을 참조합니다. (링크)[https://www.mongodb.com/docs/manual/introduction/]
* 일단 설치되면 MongoDB를 시작하는 방법은 다음과 같습니다.
  * ${MONGO_HOME}/bin/mongod
* STS에서 Spring 프로젝트를 생성하려면
  * 생략
  * pom.xml 파일 종속성 요소에 다음을 추가하십시오
  * pom.xml의 Spring 버전을 다음과 같이 변경하세요
```xml
<spring.framework.version>5.3.17</spring.framework.version>

<dependencies>
  <!-- other dependency elements omitted -->
  <dependency>
    <groupId>org.springframework.data</groupId>
    <artifactId>spring-data-mongodb</artifactId>
    <version>3.2.10</version>
  </dependency>
</dependencies>
```
* Maven용 Spring Milestone 저장소의 다음 위치를 <dependences/> 요소의 동일한 수준으로 추가하세요
```xml
<repositories>
  <repository>
    <id>spring-milestone</id>
    <name>Spring Maven MILESTONE Repository</name>
    <url>https://repo.spring.io/libs-milestone</url>
  </repository>
</repositories>
```
* 추가적인 정보를 보기위해서 log4j.properties 파일을 다음과 같이 편집하세요
```java
log4j.category.org.springframework.data.mongodb=DEBUG
log4j.appender.stdout.layout.ConversionPattern=%d{ABSOLUTE} %5p %40.40c:%4L - %m%n
```
* 그런다음에 Person 클래스를 만들 수 있습니다. 
```java
package org.spring.mongodb.example;

public class Person {

  private String id;
  private String name;
  private int age;

  public Person(String name, int age) {
    this.name = name;
    this.age = age;
  }

  public String getId() {
    return id;
  }
  public String getName() {
    return name;
  }
  public int getAge() {
    return age;
  }

  @Override
  public String toString() {
    return "Person [id=" + id + ", name=" + name + ", age=" + age + "]";
  }
}
```
* 다음의 간단한 예에서도 주의 할 점이 있습니다.
  * 표준 `com.mongodb.client.MongoClient` 객체와 데이터 베이스 이름을 사용해서 `MongoTemplate` 인스턴스화 할 수 있습니다.
  * mapper는 추가 메타 데이터 없이 POJO 객체에 대해서 동작합니다
  * 규약은 id 필드를 처리하는 데 사용되며 데이터 베이스에 저장될때 ObjectId로 변환합니다. 
  * 매핑 규칙은 필드 접근에 사용 할수 있습니다. Person 클래스에는 getter만 있습니다. 
  * 생성자 인수 이름이 저장된 문서의 필드의 이름과 일치하면 객체를 인스턴화 하는데 사용됩니다. 
```java
package org.spring.mongodb.example;

import static org.springframework.data.mongodb.core.query.Criteria.where;

import org.apache.commons.logging.Log;
import org.apache.commons.logging.LogFactory;
import org.springframework.data.mongodb.core.MongoOperations;
import org.springframework.data.mongodb.core.MongoTemplate;
import org.springframework.data.mongodb.core.query.Query;

import com.mongodb.client.MongoClients;

public class MongoApp {

  private static final Log log = LogFactory.getLog(MongoApp.class);

  public static void main(String[] args) throws Exception {

    MongoOperations mongoOps = new MongoTemplate(MongoClients.create(), "database");
    mongoOps.insert(new Person("Joe", 34));

    log.info(mongoOps.findOne(new Query(where("name").is("Joe")), Person.class));

    mongoOps.dropCollection("person");
  }
}
```

### Repository 예제
* (다음 링크 참조)[https://github.com/spring-projects/spring-data-examples]

### Spring에서 MongoDB에 연결하기
* 여기 부터 시작 : https://docs.spring.io/spring-data/mongodb/docs/3.2.10/reference/html/#introduction

## 컬렉션에서 finding 및 업데이트 하기 
* `findAndModify` 메서드를 사용하면 단일 작업으로 문서를 업데이트하고 이전 또는 새로 업데이트된 문서를 반환할 수 있습니다. 
* MongoTemplate은 Query 및 Update 클래스 사용하고 Document에서 POJO로 변환하는 4개의 `findAndModify` 오버로드 제공합니다. 
```java
<T> T findAndModify(Query query, Update update, Class<T> entityClass);

<T> T findAndModify(Query query, Update update, Class<T> entityClass, String collectionName);

<T> T findAndModify(Query query, Update update, FindAndModifyOptions options, Class<T> entityClass);

<T> T findAndModify(Query query, Update update, FindAndModifyOptions options, Class<T> entityClass, String collectionName);
```
* 다음 예제에서 몇가지 Person 객체를 컨테이너에 삽입하고 findAndUpdate 작업을 수행합니다.
```java
template.insert(new Person("Tom", 21));
template.insert(new Person("Dick", 22));
template.insert(new Person("Harry", 23));

Query query = new Query(Criterial.where("firstName").is("Harry"));
Update update = new Update().inc("age", 1);

Person oldValue = template.update(Person.class)
    .matching(query)
    .apply(update)
    .findAndModifyValue(); // 이전 Person Object를 리턴합니다.

assertThat(oldValue.getFirstName()).isEqualTo("Harry");
assertThat(oldValue.getAge()).isEqualTo(23);

Person newValue = template.query(Person.class)
  .matching(query)
  .findOneValue();

assertThat(newValue.getAge()).isEqualTo(24);

Person newestValue = template.update(Person.class)
    .maching(query)
    .apply(update)
    .withOptions(FindAndModifyOptions.options().returnNew(true))
    .findAndModifyValue(); // 업데이트 된 Person Object를 리턴합니다.

assertThat(newestValue.getAge()).isEqualTo(25);

Person upserted = template.update(Person.class)
  .matching(new Query(Criteria.where("firstName").is("Mary")))
  .apply(update)
  .withOptions(FindAndModifyOptions.options().upsert(true).returnNew(true))
  .findAndModifyValue()

assertThat(upserted.getFirstName()).isEqualTo("Mary");
assertThat(upserted.getAge()).isOne();
```

* `FindAndModifyOptions` 메서드는 returNew, upsert, remove 옵션을 설정할 수 있습니다.

## appendix
### POJO
* Plain Old Java Object, 간단히 POJO는 오래된 방식의 간단한 자바 오브젝트입니다. 
* JAVA EE나 중량 프레임 워크들이 프레임 워크에 종속된 "무거운" 객체를 만드는 것에 반대하기 위해서 사용된 용어입니다.

### IoC(Inversion Of Control) container
* IoC(역제어)란  프로그래머가 작성한 프로그램이 재사용 라이브러리의 흐름 제어를 받게 되는 소프트 웨어 디자인 패턴을 말합니다. 
  * 전통적인 프로그램에서 흐름은 프로그래머가 작성한 프로그램이 외부 라이브러리 코드를 호출해서 이용합니다. 
* 하지만 IoC에서는 외부 라이브러리 코드가 프로그래머가 작성한 코드를 호출 합니다. 
* 다음과 같은 목적은 아래와 같스빈다. 
  * 작업을 구현하는 방식과 작업 수행 자체를 분리합니다.
  * 모듈을 제작할 때, 모듈과 외부 프로그램의 결합에 대해 고민할 필요 없이 모듈의 목적에 집중할 수 있습니다.
  * 다른 시스템이 어떻게 동작 할지에 대해 고민할 필요 없이 미리 정해진 협약으로 동작하면 됩니다.
  * 모듈을 바꾸어도 다른 시스템에 부작용을 일으키지 않습니다. 
* 참조
  * https://ko.wikipedia.org/wiki/%EC%A0%9C%EC%96%B4_%EB%B0%98%EC%A0%84