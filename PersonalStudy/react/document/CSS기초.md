* http://tcpschool.com/css/intro를 기반으로 학습을 진행합니다. 
  
## CSS 개요
* CSS란 웹 페이지의 스타일과 내용적인 부분을 서로 분리해 놓은 서식을 스타일 시트라고 함
* CSS(Cascading Style Sheet)의 약자입니다. 
* CSS를 사용해서 웹 사이트의 스타일을 일관성 있게 유지하도록 함 
  
## CSS 문법
### id 선택자
* id를 통해서 CSS와 요소를 연결합니다. 아래의 예제에서는 `heading`를 통해서 연결합니다. 
* 일반적으로 하나의 요소에 하나의 아이디를 연결합니다. 
```css
<style>
    #heading { color: teal; text-decoration: line-through; }
</style>
...
<h2 id="heading">이 부분에 스타일을 적용합니다.</h2>
```
### class 선택자
* 클래스 선택자는 특정 집단의 여러 요소를 한번에 선택할 때 사용합니다. 
```css
<style>
    .headings { color: lime; text-decoration: overline; }
</style>
...
<h2 class="headings">이 부분에 스타일을 적용합니다.</h2>
<p>class 선택자를 이용하여 스타일을 적용할 HTML 요소들을 한 번에 선택할 수 있습니다.</p>
<h3 class="headings">이 부분에도 같은 스타일을 적용합니다.</h3>
```
## CSS 적용 방식
### 인라인 스타일 
* HTML 요소 내부에 style 속성을 사용하는 방식 
```html
<body>
    <h2 style="color:green; text-decoration:underline">
        인라인 스타일을 이용하여 스타일을 적용하였습니다.
    </h2>
</body>
```
### 내부 스타일 시트
* html의 head 태그에서 style 태그를 사용해서 CSS 스타일 적용 방식
```html
<head>
    <style>
        body { background-color: lightyellow; }
        h2 { color: red; text-decoration: underline; }
    </style>
</head>
```
### 외부 스타일 시트 
* 외부의 CSS 파일을 기준으로 적용합니다. 
### 스타일 적용 우선순위
1. 인라인 스타일 (HTML 요소 내부에 위치함)
2. 내부 / 외부 스타일 시트 (HTML 문서의 head 요소 내부에 위치함)
3. 웹 브라우저 기본 스타일

### CSS 기본속성은 웹사이트 참조
* http://tcpschool.com/css/css_basic_colors
  
### CSS 크기 단위
#### 백분율
* 기본 크기를 100%로 놓고 상대적인 크기를 구함
#### 배수 단위
* 해당 글꼴의 기본 크기를 1em으로 놓고 상대적인 크기 구함
#### 픽셀 단위 
* 스크린 픽셀을 기준으로 하는 절대적인 크기
  
### CSS 크기

|속성|설명|
|:--|:--|
|height|해당 HTML 요소의 높이를 설정함.|
|width|해당 HTML 요소의 너비를 설정함.|
|max-width|해당 HTML 요소가 가질 수 있는 최대 너비(width)를 설정함.|
|min-width|해당 HTML 요소가 가질 수 있는 최소 너비(width)를 설정함.|
|max-height|해당 HTML 요소가 가질 수 있는 최대 높이(height)를 설정함.|
|min-height|해당 HTML 요소가 가질 수 있는 최소 높이(height)를 설정함.|

### CSS 박스모델
* 박스모델은 HTML요소를 패딩(padding), 테두리(border), 마진(margin), 내용(content)으로 구분함
* CSS에서 설정한 height, width는 `content`를 뜻함, 나머지 요소는 따로 정의해줘야함
|속성|설명|
|:--|:--|
|내용(content)|실질적인 내용이 들어가는 부분|
|패딩(padding)|내용과 테두리의 간격임 패딩은 눈에 보이지 않음|
|테두리(border)|내용과 패딩을 감싸는 부분 |
|마진(margin)|이웃하는 HTML요소들과의 간격|

#### 박스 모델 설정 예제
```css
.pad {
    /* padding 설정 */
    padding-top: 50px;
    padding-right: 10px;
    padding-bottom: 30px;
    padding-left: 100px;

    /* padding 축약 버전*/
    padding: 20px 50px 30px 50px;
}
```
