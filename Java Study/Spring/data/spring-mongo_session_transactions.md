## 번역링크 
* https://docs.spring.io/spring-data/mongodb/docs/3.2.10/reference/html/


## MongoDB Sessions
* 버전 3.6부터 MongoDB는 세션 개념을 지원합니다.
* 세션을 사용하면 인과 관계의 순서로 작업을 보장하는 MongoDB의 Causal Consistency 모델을 사용합니다.
* 세션은 ServerSession 인스턴스와 ClientSession 인스턴스로 나뉩니다. 여기서는 세션이란 ClientSession을 뜻합니다. 
* 주의 사항
  * 클라이언트 세션 내의 작업은 외부의 세션 작업과 격리되지 않습니다.
* MongoOperations과 ReactiveMongoOperations는 모두 ClientSession을 작업에 연결하기 위한 게이트 웨이 메서드를 제공합니다. 
* MongoCollection 및 MongoDatabase는 MongoDB의 컬렉션 및 데이터베이스 인터페이스를 구현하는 세션 프록시 객체를 사용하므로 session 객체를 추가할 필요가 없습니다.

### 동기적 ClientSession support.
* 다음 예는 세션 사용법을 보여 줍니다.
```java
// MongoOperations에 ClientSession 함께 사용하기 
ClientSessionOptions sessionOptions = ClientSessionOptions.builder()
  .causallyConsistent(true)
  .build();

// 서버에서 새 세션을 얻습니다.
ClientSession session = client.startSession(sessionOptions);
template.withSession(() -> session)
  .execute(action ->{
    // 이전과 같이 MongoOperation 메서드를 사용하면 ClientSession이 자동으로 적용됩니다.
    Query query  = query(where("name").is("Durzo Blint"));
    Person durzo = action.findOne(query, Person.class);

    Person azoth = new Person("Kylar Stern");
    azoth.setMaster(durzo);
    action.insert(azoth);
    // 세션을 닫아야 합니다. 
    return azoth;
  });

// 세션을 닫습니다. 
session.close();
```
### 비동기적 ClientSession support
* 동기 작업와 동일한 building block을 사용합니다. 
```java
ClientSessionOptions sessionOptions = ClientSessionOptions.builder()
    .causallyConsistent(true)
    .build();
// Publisher 형태의 Session을 얻습니다. 
Publisher<ClientSession> session = client.startSession(sessionOptions); 

template.withSession(session)
    .execute(action -> {

        Query query = query(where("name").is("Durzo Blint"));
        return action.findOne(query, Person.class)
            .flatMap(durzo -> {

                Person azoth = new Person("Kylar Stern");
                azoth.setMaster(durzo);
                // 이전과 같이 자동으로 session이 적용됩니다. 
                return action.insert(azoth);                            
            });
    }, ClientSession::close)                                            
    .subscribe();   
    // 구독 전에는 아무일도 일어나지 않습니다. 
```
* 실제 세션을 제공하는 Publisher를 이용해서 실제 구독 시점까지 세션 획득을 연기 할 수 있습니다. 
* 그래도 완료되면 세션을 닫아야 오래된 세션이 서버에 문제를 일으키지 않습니다. 
* doFinally 훅을 사용해서 세션이 더이상 필요하지 않을 때 ClientSession#close()를 호출 합니다. 
* 세션 자체에 대해서 더많은 작업을 원하는 경우 드라이버를 통해서 ClientSession을 얻고 Supplier를 통해서 제공할 수 있습니다. 
* 정보
  * ClientSession의 Reactive 사용은 Template API 사용으로 제한됩니다. 
  * 현재 Reactive Repository와 통합할 계획은 없습니다. 




## MongoDB Transactions
* MongoDB는 버전 4부터 트랜잭션을 지원합니다. 
* 트랜젝션은 세션 위에서 구축되므로 결과적으로 활성화된 ClientSession이 필요합니다. 
* 정보
  * 애플리케이션 컨텍스트 내에서 MongoTransactionManager를 지정하지 않으면 트랜잭션 지원이 비활성화 됩니다. 
  * setSessionSynchronization(ALWAYS) 를 사용해서 participate in ongoing non-native MongoDB transactions를 사용할 수 있습니다. 
* 트랜잭션을 완전히 프로그래밍 방식을 제어하려면 MongoOperations에서 세션 콜백을 사용 할 수 있습니다. 
```java
// 클라이언트 세션을 얻습니다.
ClientSession session = client.startSession(options);

template.withSession(session)
  .execute(action -> {

    // transaction을 시작합니다.
    session.startTransaction();

    try{
      Step step = //...;
      action.insert(step);

      process(step);

      action.update(Step.class).apply(Update.set("state", //... ))

      // 모든 것이 예상대로 작동 되면 변경사항을 커밋합니다. 
      session.commitTransaction();
    } catch( RuntimeException e) {
      // 문제가 발생하면 모든 것을 롤백합니ㅏ. 
      session.abortTransaction();
    }
    // 세션은 꼭 닫아야 합니다. 
  }, ClientSession::close)
```
* 앞의 예를 사용하면 세션을 통해서 모든 server call이 콜백에 안에서 보장되도록 MongoOperations 인스턴스를 scope 안에서 세션을 사용하는 동안 트랜 젝션 동작을 완전히 제어 할 수 있습니다. 
* 이 접근 방식에서 발생하는 약간의 오버헤드를 제거하기 위해서 TransactionTemplate을 사용해서 수동 트랜잭션 flow의 노이즈를 제거 할 수 있습니다. 

### Transactions with TransactionTemplate
* Spring Data MongoDB 트랜잭션은 TransactionTemplate을 지원합니다.
* 런타임중에 MongoTemplate의 상태를 변경하면 스레딩 및 가시성 문제가 발생 할 수 있습니다. 
```java
// 템플릿 API 구성중에 트랜젝션 동기화를 활성화 합니다. 
template.setSessionSynchronization(ALWAYS);
//...

// PlatformTransactionManager를 사용해서 TransactionTemplate 생성
TransactionTemplate txTemplate = new TransactionTemplate(anyTxManager);
txTemplate.execute(new TransactionCallBackWithoutResult() {

  // 콜백 내에서 ClientSession 및 트랜젝션이 이미 등록되어 있습니다.
  @Override
  protected void doInTransactionWithoutResult(TransactionStatus status){
    Step step = // ...;
    template.insert(step);

    process(step);

    template.update(Step.class).apply(Update.set("state", // ...
  }
}
)
```

### Transactions with MongoTransactionManager
* MongoTransactionManager는 잘 알려진 Spring 트랜젝션 지원의 관문입니다. 
* 이를 통해서 애플리케이션은 Spring의 managed transaction 기능들을 사용할 수 있습니다. 
* MongoTransactionManager는 ClientSession을 스레드에 바인딩합니다.
* MongoTemplate은 세션을 감지하고 트랜잭션과 연결된 리소스에서 동작하고 다른 진행 중인 트랜젝션에도 참여할 수 있습니다. 
* 정보
  * `@Transactional(readOnly = true)`는 MongoTransactionManager에 시작하는 트랙잭션에 ClientSession을 추가하도록 조언합니다. 
```java
@Configuration
static class Config extends AbstarctMongoClientConfiguration {
  // 애플리케이션 컨텍스트에 MongoTransactionManager를 등록합니다. 
  @Bean
  MongoTransactionManager transactionManager(MongoDatabaseFactory dbFactory){
    return new MongoTransactionManager(dbFactory);
  }

  //...

  @Component
  public class StateService {

    // 메서드로 트랜젝션을 표시합니다. 
    @Transactional
    void someBusinessFunction(Step step) {
      template.insert(step);

      process(step);

      template.update(Step.class).apply(Update.set("state")) //...
    }
  }
}
```

### Reactive Transactions
* 생략

### Transactions with TransactionalOperator
* 생략 

### Transactions with ReactiveMongoTransactionManager
* ReactiveMongoTransactionManager는 잘 알려진 Spring의 트랜젝션 지원의 관문입니다. 
* ReactiveMongoTransactionManager는 ClientSession을 subscriber Context에 바인딩 합니다. 
* MongoTemplate은 세션을 감지하고 트랜잭션과 연결된 리소스에서 동작하고 다른 진행 중인 트랜젝션에도 참여할 수 있습니다. 
```java

// ReactiveMongoTransactionManager 를 어플리케이션 컨텍스트에 등록합니다. 
@Configuration
static class Config extends AbstaractMongoClientConfiguration {
  @Bean
  ReactiveMongoTransactionManager transactionManager(ReactiveDatabaseFactory factory) {
    return new ReactiveMongoTransactionManager(factory);
  }

  //...
}


@Service 
public class StateService {
  // transactional 사용
  @Transactional
  Mono<UpdateResult> somoBusinessFunction(Step step) {
    return tempate.insert(step)
      .then(process(step))
      .then(tempate.update(Step.class).apply(Update.set("state", ...));
  };
}
```

### 트랜잭션 내부의 특수 동작
* 트랜잭션 내부에서 MongoDB 서버는 약간 다른 동작을 합니다. 
#### Connection Settings
* MongoDB 드라이버는 드라이버를 자동 감지모드로 전환하는 특정한 replica set name 구성 옵션을 제공합니다. 
* 이 옵션은 트랜젝션 중에서 primary replica set 노드 및 명령 라우팅 식별하는데 도움이 됩니다. 
* 정보
  * MongoDB URI에 replicaSet을 추가해야 합니다. 자세한 내용은 연결 문자열 옵션을 참조하십시오.
#### Collection Operations
* MongoDB는 트랜젝션 내에서 컬랙션 생성과 같은 컬랙션 작업을 지원하지 않습니다. 

#### Transient Errors
* MongoDB는 트랜젝션 작업중에 발생한 오류에 특수 레이블을 추가 할 수 있습니다. 
* 이것은 작업을 재시도하는 것만으로도 사라질 수있는 있는 일시적인 오류를 나타 낼 수 있습니다. 
* 이러한 목적으로 Spring Retry를 적극 권장합니다. 
* 그럼에도 불구 하고 MongoDBㅊ팜조 매뉴얼에 설명된 대로 재시도 커밋 작업 동작을 구현하기 위해서 MongoTransactionManager#doCommit을 재정의할 수 있습니다. 

#### Count
* MongoDB count는 트랜잭션 내에서 실제 상황을 반영 하지 않을 수 있는  collection 통계에 따라 작동됩니다.
* 서버는 다중 document 트랜잭션내에서 count 명령을 실행 할 때 오류 50851로 응답합니다. 
* MongoTemplate이 활성 트랜잭션을 감지하면 노출된 모든 count()메서드가 변환되고 aggregation 프레임워크에서 사용는 $match와 $count 연산자로 위임됩니다. collation와 같은 쿼리는 보존 됩니다.
https://docs.spring.io/spring-data/mongodb/docs/3.2.10/reference/html/#mongo.transactions.reactive-tx-manager
