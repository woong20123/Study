## 클래스의 메모리 구조
* static 멤버 변수는 클래스 자체에 종속되지 클래스 객체에 종속되지 않음
  * 메모리도 전역객체처럼 관리됨 
* 클래스 객체의 메모리 구조는 선언순서와 일치함
* 구조체 맞춤 설정에 따라서 내부구조 달라짐
  * 기본값은 /Zp8
* 구조체 맞춤 설정을 하는 이유
  * CPU명령는 16바이트로 맞춘 데이터가 필요함
  * CPU의 캐시 단위로 데이터의 크기를 맞추는 것이 효율적임 
  * https://docs.microsoft.com/ko-kr/cpp/cpp/align-cpp?view=msvc-160
  
* CACHE_ALIGN 명령어
  * 예제를 참조하자
  * https://jungwoong.tistory.com/42?category=1067195 

* 클래스의 크기 
```c++ 
// 아무 값도 없지만 0이 될 수 없어 sizeof = 1
class CEmpty{
public :
    CEmpty() {};
    ~CEmpty() {};
};


// 가상 함수 테이블 포인터를 보유하기 때문에 sizeof = 8 (64비트 경우)
class CVirtualEmpty
{
public:
    CVirtualEmpty() {};
    virtual ~CVirtualEmpty() {};
};
```

## 생성자&소멸자
* 생성자, 소멸자가 명시적이지 않은 경우 특별히 필요하지 않다면 굳이 암시적으로 생성 안함
* 만약 부모 클래스에 생성자나 소멸자가 명시적으로 선언되었다면 자식은 암시적으로 생성됨

### 생성자와 소멸자의 호출 순서
```c++
class CParent{
    // 생성자 & 소멸자 선언
}

class CChild : public CParent{
    // 생성자 & 소멸자 선언
}
```

#### 로직 수행 순서
1. CParent 생성자 로직 수행
2. CChild 생성자 로직 수행
3. CChild 소멸자 로직 수행
4. CParent 소멸자 로직 수행

#### 실제 내부 수행 순서
1. CChild 생성자 호출
   1. CParent 생성자 호출
   2. 멤버가 클래스 타입인 경우 생성자 호출
   3. CChild 생성자 로직 수행
2. CChild 소멸자 호출
   1. CChild 소멸자 로직 수행
   2. 멤버가 클래스 타입인 경우 소멸자 호출
   3. CParent 소멸자 호출

### virtual 소멸자 
* 가상 함수 하나라도 존재하게 되면 클래스 객체는 메모리의 0번째 오프셋에 vfptr이 생김
* 실제 가상 함수 테이블 구조를 도식화 하기 
* RTTI 정보
* 
```c++
class CParent{
    CParent() { std::cout << "CParent  생성자" << std::endl;}
    virtual ~CParent() { std::cout << "CParent  소멸자" << std::endl;}
}

class CChild : public CParent{
    CChild() { std::cout << "CChild  생성자" << std::endl;}
    virtual ~CChild() { std::cout << "CChild  소멸자" << std::endl;}
}

void main(){
{
  CChild c;
}

void main(){
    // Child 객체 생성 
    CParent * pParent = new Child;

    // 만약 CParent의 소멸자가 virtual이 아니라면 가상 테이블이 없기 때문에 Child 소멸자 호출되지 않음 
    delete pParent;
}

class CTest{}
CTest t = t0;  // 복사 생성자 호출

CTest t2; t2 = t0;  // 복사 대입 연산자 호출
```