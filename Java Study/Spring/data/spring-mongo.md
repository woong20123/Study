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