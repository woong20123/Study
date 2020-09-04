# 문서정리

## JAVA Reference 사이트
* https://docs.oracle.com/en/java/javase/14/docs/api/index.html

## 함수 정리
* module
  * 명명된 자체 설명 및 데이터의 모음
  * Java 클래스 및 인터페이스를 포함하는 패키지 셋트로 구성
* package
  * Java 클래스를 유사한 네임스페이스로 구성하여 제공하는 기술 
* Class.forName
  * 모듈 : java.base, 패키지 : java.lang
  
|메서드|설명|
|:---:|:---:|
|forName​(Module module, String name)|Returns the Class with the given binary name in the given module.|
|forName​(String className)|Returns the Class object associated with the class or interface with the given string name.|
|forName​(String name, boolean initialize, ClassLoader loader)|Returns the Class object associated with the class or interface with the given string name, using the given class loader.|
```java 
Class.forName("com.mysql.jdbc.Driver");
```


# 디자인 패턴
## 템플릿 메서드 패턴 
  * 상속을 통한 슈퍼 클래스의 기능을 확장할 때 사용하는 가장 대표적인 방법
  * 변하지 않는 기능은 슈퍼 클래스에 만들어 두고 변경되는 기능은 상속을 통해 지원
  * 슈퍼 클래스는 디폴트 기능을 정의해두거나 선택적으로 구현할 수 있는 훅 메서드를 지원합니다.
```java
public abstract class Super{
    public void templateMethod(){
        // 기본 알고리즘
        hookMethod();
        abstractMethod();
    }
    protected void hookMethod() {} // 선택적으로 오버라이드 가능
    public abstract void abstractMethod(); // 반드시 구현 필요
}

public class Sub1 extends Super{
    protected void hookMethod(){

    }

    public void abstractMethod(){

    }
}
```

## REST(Representational State Transfer) 이란?
### 월드 와이어 웹 같은 분산 하이퍼미디어 시스템을 위한 소프트웨어 아키텍쳐의 한 형식
### REST 아키텍쳐에 적용되는 6가지 조건
#### 클라이언트/서버 구조 : 일관적인 인터페이스로 분리 되어야 한다.
#### 무상태(stateless) : 각 요청 간 클라이언트의 context가 서버에 저장되어서는 안된다. 
  


