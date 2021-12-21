## Mockito란?
* Mockito는 개발자가 동작을 직접 제어할 수 있는 가짜 객체를 지원하는 테스트 프레임워크입니다. 
* 여러 객체간의 의존성이 존재하는데 이러한 의존성을 단위 테스트를 작성하는 것을 개변함
  
## Mockito 사용법
### Mock 객체의 의존성 주입
* Mock 객체에 의존성을 주입하기 위해서는 3가지 어노테이션을 사용함
  * @Mock : Mock객체를 만들어 반환해주는 어노테이션
  * @Spy : Stub 하지 않은 메소드들을 원본 메소드 그대로 사용하는 어노테이션 
  * @InjectMocks : @Mock또는 @Spy로 생성된 가짜 객체를 자동으로 주입시키는 어노테이션 
  * @MockBean : Mock객체를 spring ApplicationContext에 넣어줍니다.
* 예를 들어 UserController에 대한 단위 테스트를 작성 할 때 UserService를 사용하고 있다면 다음과 같이 설정
  * @Mock 어노테이션을 통해서 가짜 UserService를 만들고 @InjectMocks을 통해서 UserController를 주입시킴
  
 
### Stub
* 의존성 있는 객체는 가짜 객체를 주입하여 어떤 결과를 반환하라고 정해진 답변을 준비시킵니다. 
  * doReturn() : mock객체가 특정한 값으로 반환하는 경우 
  * doNothing() : mock객체가 아무것도 반환하지 않는 경우
  * doThrow() : mock객체가 예외를 발생시키는 경우 
```java
// UserService가 findAllUser() 호출 시에 빈 ArrayList를 반환해야 한다면 다음과 같이 doReturn을 사용
doReturn(new ArrayList()).when(userService).findAllUser();
```

### Mockito와 JUnit의 결합
* Mockito가 JUnit과 결합되기 위해서는 다음과 같은 작업이 필요합니다.
* JUnit4에서는 @RunWith(MokitoJUnitRunner.class)를 붙여서 연동
  * @RunWith
    * JUnit의 테스트 러너를 확장하는 방법중 하나입니다. 
* JUnit5(SpringBoot2.2.0)에서는 @ExtendWith(MockitoExtension.class) 사용
  * JUnit5에서 지원함
  * @ExtendWith
    * org.junit.jupiter.api.extension.Extension를 상속한 인터페이스로 기능을 구현합니다. 
    * 여러번 중복 사용 가능
    * 메타 애노테이션을 지원함


### 참조문서
* https://code.google.com/archive/p/mockito/wikis/MockitoFeaturesInKorean.wiki