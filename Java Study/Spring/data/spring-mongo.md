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
* 


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