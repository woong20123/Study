## jQuery
* 제이쿼리는 자바스크립트 언어를 간편하게 사용 할 수 있도록 단순화 시킨 오픈 소스 자바스크립트

## jQuery 적용 하는 법
* 파일을 다운 받아서 태크 삽입
```jsp
<head>
    <script src="/파일경로/제이쿼리파일명.js"></script>
</head>
```
* CDN을 이용해서 설정하는 방법
```jsp
1. jQuery.com CDN : <script src="https://code.jquery.com/jquery-1.12.4.min.js"></script>
2. 구글 CDN       : <script src="https://ajax.googleapis.com/ajax/libs/jquery/1.12.4/jquery.min.js"></script>
3. MS CDN         : <script src="http://ajax.aspnetcdn.com/ajax/jQuery/jquery-1.12.4.min.js"></script>
4. CDNJS CDN      : <script src="https://cdnjs.cloudflare.com/ajax/libs/jquery/1.12.4/jquery.min.js"></script>
5. jsDelivr CDN   : <script src="https://cdn.jsdelivr.net/jquery/1.12.4/jquery.min.js"></script>
```

## 제이쿼리 문법
* `$(선택자).동작함수();` 기본 문법입니다.
* `$`는 제이쿼리를 의미하고, 제이쿼리에 접근할 수 있도록 합니다. 

## $()함수
* $()함수는 HTML의 요소를 제이쿼리에서 이용할 수 있는 형태로 생성함

## Document 객체의 ready() 메소드
* 자바스크립트 코드는 웹브라우져가 문서의 모든 요소를 로드한 후에 실행되어야 합니다. 
* window.onload를 통해서 문서가 로드된후 코드 실행
```jsp
window.onload = function() {
    // 코드
}
```

* 마찬가지로 제이쿼리에서는 Document객체의 ready 메서드를 이용해서 같은 결과를 보장함
```jsp
$(document).ready(function() {
    // 제이쿼리 코드;
})

// 또는
$(function() {
    제이쿼리 코드;
});
```

## 참조 경로
* http://www.tcpschool.com/jquery/jq_basic_syntax