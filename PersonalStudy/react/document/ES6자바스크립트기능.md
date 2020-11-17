## 변수를 정의하는 새로운 방법 : const, let
* ES5전까지는 var를 이용해서 변수를 정의 
### var의 문제
* var는 함수 스코프를 가짐, var 키워드 없다면 전역 변수를 가짐
* var로 정의된 변수는 호이스팅(hoisting) 문제 발생
* var변수는 재정의할 수 있음
### const, let
* const, let은 블록 스코프를 가짐 
* const 변수는 재할당을 불가능 하도록 만듭니다. 

## 강화된 함수의 기능
### 매개변수 
#### 매개변수 기본값
```js
// a = 1 가 매개변수 기본값
function printLog(a = 1){
    ...
}
```
#### 나머지 매개변수
```js
function printLog(a, ...rest){
    console.log({a, rest});
}
printLog(1,2,3); // {a : 1, rest : [2,3] }
```
#### 명명된 매개변수 
```js
const numbers = [10, 20, 30, 40];
const result1 = getValues(numbers, 5, 25);
// 명명된 매개변수 사용하기 
const result2 = getValues({numbers, greaterThan:5, lessThan:25});
```
#### 화살표 함수(Arrow function)
```js
const add = (a, b) => a + b;
add(2, 3); // => 5

const addAndReturnObject = (a, b) => ({ result: a + b});
addAndReturnObject(1,3).result // => 4

// 여러줄 이상인 경우 `{`,`}`를 사용합니다.
const add = (a, b) => {
    if(a <= 0 || b <= 0){
        throw new Error('must be positive number');
    }
    return a + b;
}
```

## 비동기 프로그래밍
### 프로미스(promise)
* 프로미스는 비동기 상태를 값을 다룰 수 있는 객체임 
* 이전에는 콜백 함수를 사용했지만 순서를 읽기가 너무 힘듬
### 프로미스의 세 가지 상태
* 대기중(pending) : 결과를 기다리는 중
* 이행됨(fulfilled) : 수행이 끝났고 결과값을 가짐
* 거부됨(rejected) : 수행이 비정상적으로 끝남
### 프로미스 예제 코드
#### 프로미스 생성 예
```js
// new를 통해서 생성하며 resolve, reject 콜백 함수를 등록한다. 
const p1 = new Promise((resolve, reject) => {
    // ...
    // 성공하면 resolve(data) 호출
    // 실패하면 or reject('error message')
});
const p2 = Promise.reject('error message');
const p3 = Promise.resolve(param);

// 실제 호출 예제
const p1 = Promise.resolve(123);
console.log(p1 !== 123);    // true
const p2 = new Promise(resolve => setTimeout(() => resolve(10), 1));
console.log(Promise.resolve(p2) === p2); // true
```
#### 프로미스 then 사용 예제
```js
requestData().then(onResolve, onReject);                // 처리되면 onResolve 함수 호출, 실패시 onReject 함수 호출
Promise.resolve(123).then(data => console.log(data));   // 123 출력
Promise.reject('err').then(null, error => console.log(error));  // 에러발생
```
* 연속해서 then으로 메서드 호출
```js
requestData1()
    .then()(data => {
      console.log(data)  ;
      return requestData2();
    })
    .then(data => {
        return data + 1; 
    })
    .then(data => {
        throw new Error('some error');
    })
    .then(null, error => {
        console.log(error);
    });
```
#### 프로미스 catch 사용 예제
* catch는 프로미스 수행중에 발생한 예외를 처리하는 메서드 
```js
Promise.reject(1).then(null, error => {
    console.log(error);
});
Promise.reeject(1).catch( error => {
    console.log(error)
});
```

### 프로미스 병렬처리
* `Promise.all`은 여러개의 프로미스를 병렬로 처리할때 사용
* then을 사용하면 순차적으로 실행 
```js
Promise.all([requestData1(), requestData2()]).then( ([data1, data2]) =>{
    console.log(data1, data2);
});
```

