# Auto Generated Values
#### @GeneratedValue Annotation으로 필드를 표시하면 해당 필드에 대한 값이 자동으로 생성되도록 지정합니다. 
#### 이것은 주로 primary key 필드를 위한 값이지만 ObjectDB는 키가 아닌 numberic persistens 필드에도 지원합니다.
#### 몇몇의 다른 값 생성 전략들을 아래에 설명된 대로 사용할 수 있습니다.

## Auto Strategy
* ObjectDB는 모든 데이터베이스에 대해 특별한 전역의 번호 생성기를 유지합니다.
* 이 숫자 생성기는 primary key 필드가 정의되지 않은 entity 개체에 대한 자동 객체 ID를 생성하는 데 사용됩니다.
* @GeneratedValue를 AUTO 전략을 사용해서 primary key 필드의 숫자 값을 생성하는 데도 같은 숫자 생성기가 사용됩니다. 
```java
@Entity
public class EntityWithAutoId1 {
    @Id @GeneratedValue(strategy=GenerationType.AUTO) long id;
     :
}
```
* AUTO는 기본전략이므로 다음의 정의와 동일합니다.

```java
@Entity
public class EntityWithAutoId2 {
    @Id @GeneratedValue long id;
     :
}
```
* commit하는 동안 AUTO전략은 전역 번호 생성기를 사용해서 모든 새 항목 개체에 대한 기본 키를 생성합니다. 
* 이렇게 생성된 값은 데이터 베이스 레벨에서 고유하며 절대 재사용하지 않습니다.(중복X)

## The Identity Strategy  
* IDENTITY 전략은 AUTO 전략과 매우 유사합니다. 
```java
@Entity
public class EntityWithIdentityId {
    @Id @GeneratedValue(strategy=GenerationType.IDENTITY) long id;
     :
}
```
* IDENTITY 전략은 모든 새로운 객체가 commit하는 동안 자동값을 생성합니다. 
* 차이점은 ID 생성기가 계층별로 관리되므로 생성된값은 계층별로 고유하다는 것입니다. 
# The Sequence Strategy
* Sequence 전략은 2부분으로 구성되는데 명명된 Sequence를 정의하고 하나이상의 클래스에 있는 하나 이상의 필드에서 이름 붙여진 sequence의 사용입니다.
* @SequenceGenerator은 시퀀스를 정의하는 데 사용되며 이름, 초기 값 및 할당 크기을 전달받습니다. 
  * 초기값의 기본 값은 1이고 할당 크기의 기본값은 50입니다.
* sequence는 응용 프로그램에서 전역적이며 하나 이상의 클래스의 하나 이상의 필드에 사용할 수 있습니다.
* sequence전략은 @GenerattedValue annotation에 사용되어서 주어진 필드를 이전에 정의된 sequence의 이름으로 연결합니다. 
```java
@Entity
// Define a sequence - might also be in another class:
@SequenceGenerator(name="seq", initialValue=1, allocationSize=100)
public class EntityWithSequenceId {
    // Use the sequence that is defined above:
    @GeneratedValue(strategy=GenerationType.SEQUENCE, generator="seq")
    @Id long id;
}
```
* AUTO 및 IDENTITY와 달리 SEQUENCE 전략은 새로운 엔티티 객체가 유지되는 즉시 자동값을 생성합니다.(commit 전에)
* primary key 값이 먼저 필요할 때 유용 할 수 있습니다. 
* 데이터베이스 서버로의 왕복을 최소화하기 위해 ID가 그룹으로 할당됩니다. 
* 각 allocation에 ID의 수는 allocationSize 속성으로 지정됩니다. 
* 지정된 할당의 일부 ID가 사용되지 않을 수 있습니다. 
* 그러므로 이 전략은 sequence 값에 gap이 없음을 보장하지 않습니다. 


## org.hibernate.id.enhanced.SequenceStyleGenerator
* sequence 데이터 베이스 구조를 기반으로 식별자 값을 생성합니다. 
* 변형의 범위는 시퀀스를 실제로 사용하는 것부터 테이블을 사용하여 시퀀스를 모방하는 것까지 다양합니다.
* 이러한 변형은 내부적으로 DatabaseStructure의 인터페이스에 의해서 캡슐화 됩니다.  

## 참조
* https://www.objectdb.com/java/jpa/entity/generated