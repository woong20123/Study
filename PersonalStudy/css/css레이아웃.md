# 사이트 : https://ko.learnlayout.com/media-queries.html

## display 프로퍼티 
* 레이 아웃을 제어하기 위한 가장 중요한 프로퍼티
* 기본값은 보통 block이나 inline
* block : 새줄에서 시작해서 좌우로 최대한 늘어남 
  * div, p, form 
* inline : 표준으로 사용한
  * span, a
* none : 엘리먼트가 마치 존재하지 않는 것처럼 페이지가 렌더링됨
## width 프로퍼티
* width를 설정하면 컨테이너의 좌우 가장 자리가 늘어나지 않도록 할 수 있음 
* 좌우 margin을 auto로 설정해서 엘리먼트를 컨테이너에 중앙으로 오도록 할 수 있음
```css
#main {
  width: 600px;
  margin: 0 auto; 
}
/*
max-width를 사용하면 브라우져가 width보다 작아져도 처리할 수 있음
*/
#main {
  max-width: 600px;
  margin: 0 auto; 
}
```
* 박스모델을 사용하면 padding과 border-width를 지정하면 지정된 너비보다 더 큰 엘리먼트가 될 수도 있습니다. 
```css
.simple {
  width: 500px;
  margin: 20px auto;
}
/*
width가 같지만 실제로 fancy가 더 큰 박스로 보여짐 
*/
.fancy {
  width: 500px;
  margin: 20px auto;
  padding: 50px;
  border-width: 10px;
}
```
* 새로운 css문법을 사용하면 해당 문제를 해결 할 수 있습니다. 
* box-sizing을 사용하면 엘리먼트의 패딩과 테두리가 더는 너비를 늘리지 않습니다.
* 이기법은 IE8+에서만 사용할 수 있음
```css
.simple {
  width: 500px;
  margin: 20px auto;
  -webkit-box-sizing: border-box;
     -moz-box-sizing: border-box;
          box-sizing: border-box;
}

.fancy {
  width: 500px;
  margin: 20px auto;
  padding: 50px;
  border: solid blue 10px;
  -webkit-box-sizing: border-box;
     -moz-box-sizing: border-box;
          box-sizing: border-box;
}

/*
아래의 코드는 모든 엘리먼트 속성에 추가할 수 있습니다. 
*/
* {
  -webkit-box-sizing: border-box;
     -moz-box-sizing: border-box;
          box-sizing: border-box;
}
```
## position 프로퍼티 
### static 
* static은 position프로퍼티의 기본 값입니다. 
### relative
* top, right, bottom, left를 지정하면 기본위치와 다르게 위치가 조정됩니다. 
```css
.relative1 {
  position: relative;
}
.relative2 {
  position: relative;
  top: -20px;
  left: 20px;
  background-color: white;
  width: 500px;
}
```
### fixed
* fixed는 뷰포트에 상대적으로 위치가 지정됩니다. 
* 페이지가 스크롤 되더라도 늘 같은 곳에 위치합니다. (UI)
* top, right, bottom, left를 사용하여 위치를 지정합니다. 
```css
.fixed {
  position: fixed;
  bottom: 0;
  right: 0;
  width: 200px;
  background-color: white;
}
```
### absolute
* 가장 가까운 곳에 위치한 조상 엘리먼트에 상대적으로 위치가 지정됨
* 만약에 조상 엘리먼트가 없다면 문서 본문을 기준으로 삼습니다.
```css
.relative {
  position: relative;
  width: 600px;
  height: 400px;
}
.absolute {
  position: absolute;
  top: 120px;
  right: 0;
  width: 300px;
  height: 200px;
}
```

## float 프로퍼티 
* float는 이미지 주위를 감싸기 위해서 만들어집니다. 
```css
img {
  float: right;
  margin: 0 0 1em 1em;
}
```
## clear 프로퍼티
```html
<div class="box">...</div>
<section>...</section>
```
* 아래 예제에서 float를 사용하면 div가 왼쪽에 떠있는 상태가 되며 section이 출력시에는 엘리먼트들이 겹쳐지는 문제가 발생합니다. 
```css
.box {
  float: left;
  width: 200px;
  height: 100px;
  margin: 1em;
}
```
* 이럴때 clear를 사용해서 깔끔하게 정리할 수 있습니다.
```css
.box {
  float: left;
  width: 200px;
  height: 100px;
  margin: 1em;
}
.after-box {
  clear: left;
}
```
* float를 통해서 전체 레이아웃을 잡을 수도있습니다. 
```css
nav {
  float: left;
  width: 200px;
}
section {
  margin-left: 200px;
}
```
## 퍼센트
* 퍼센트는 특정 엘리먼트를 담고 있는 블록의 상대적인 측정 단위입니다. 
```css
article img {
  float: right;
  width: 50%;
}

nav {
  float: left;
  width: 25%;
}
section {
  margin-left: 25%;
}
```

## 반응형 디자인 
* https://developer.mozilla.org/en-US/docs/Web/CSS/Media_Queries/Using_media_queries
* 사이트가 표시되는 브라우저와 디바이스에 반응하는 사이트를 만듭니다. 
```css
@media(min-width:600px){
    nav {
        float: left;
        width: 25%;
    }
    section { 
        margin-left: 25%
    }
}
@media(max-width:599px){
    nav li { 
        display: inline;
    }
}
```

## display프로퍼티에서 inline-block을 사용하기 
* float를 사용하는 방법은 clear를 사용해야 하기 때문에 불편합니다. 
```css
.box {
  float: left;
  width: 200px;
  height: 100px;
  margin: 1em;
}
.after-box {
  clear: left;
}
```

* inline-block를 사용하면 같은 효과를 낼 수 있습니다. 
```css
.box2 {
  display: inline-block;
  width: 200px;
  height: 100px;
  margin: 1em;
}
```
### inline-block을 사용할 때 주의점
* inline-block 엘리먼트는 vertical-align 프로퍼티의 영향을 받습니다
* 컬럼간의 공백이 있으면 틈이 생깁니다. 

## flexbox 레이아웃 모드
* 새로운 flexbox 레이아웃 모든 우리가 CSS로 레이아웃 잡는 방법을 재정의합니다. 
```css
.container {
  display: -webkit-flex;
  display: flex;
}
/* initial은 200px이며 최소 100px까지 축소됩니다. */
.initial {
  -webkit-flex: initial;
          flex: initial;
  width: 200px;
  min-width: 100px;
}
/* none은 고정 200px를 가집니다.  */
.none {
  -webkit-flex: none;
          flex: none;
  width: 200px;
}
/* flex1은 나머지 공간의 1/3을 가집니다. */
.flex1 {
  -webkit-flex: 1;
          flex: 1;
}
/* flex1은 나머지 공간의 2/3을 가집니다. */
.flex2 {
  -webkit-flex: 2;
          flex: 2;
}
```

### flexbox를 사용해서 아이템을 가운데로 지정합니다. 
```css
.vertical-container {
  height: 300px;
  display: -webkit-flex;
  display:         flex;
  -webkit-align-items: center;
          align-items: center;
  -webkit-justify-content: center;
          justify-content: center;
}
```

## css에서 class와 id 프로퍼티의 차이 
* id : 유일한 요소에 스타일을 적용합니다. `#` 사용
* class : 다수의 요소에 스타일을 적용합니다. `.` 사용