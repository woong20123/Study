# CSS3 개요
* CSS3는 이전버전과 호환되는 CSS 최신 표준 권고안

## 2DTransform
* http://www.tcpschool.com/css/css3_transform_2Dtransform
* CSS3에서는 transform 속성을 사용해서 HTML의 요소의 모양과 크기 위치등을 자유롭게 바꿀 수 있습니다. 
* transform 속성 
  * 요소를 움직입니다. 
  * 요소를 회전시킵니다. 
  * 요소의 크기를 변경합니다. 
  * 해당 요소를 기울입니다. 

## CSS 좌표 체계
-----> x축
|
|
V
y축

## 2D transform 메서드
* translate
  * 위치를 이동시킵니다. 
* rotate
  * 해당 요소를 주어진 각도만큼 회전 시킵니다. 
* scale
  * 해당 요소의 크기를 배율만큼 늘리거나 줄입니다. 
* skew
  * x나 y축을 기준으로 회전 시킵니다. 
* matrix

## 2D transform 속성 
|속성|설명|
|:--|:--|
|transform|요소에 2D또는 3D 변형을 적용함|

# transition(전환)
## transition 속성 
* transition은 css 스타일에 전환 효과를 설정합니다. 
* 전환 효과가 지속될 시간을 설정합니다. 
```css
div.keyboard {
    width: 100px;
    opacity: 0.1;
    transition: width 1s;
}
div.keyboard:hover{width: 300px; opacity: 1;}
```

## transition-timing-function 속성
* 전환 효과의 시간당 속도를 설정합니다. 
* linear : 일정한 속도로 진행됨
* ease : 기본값으로 천천히 시작되어 빨라지고 마지막에는 느려집니다.
* ease-in : 천천히 시작됨
* ease-out : 천천히 끝남
* ease-in-out : 천천히 시작되어 천천히 끝남
* cubic-bezier( n,n,n,n) : 사용자가 지정한 cubic-bazier 함수에의해서 진행됨
```css
div.keyboard {
    width: 100px;
    opacity: 0.1;
    transition: width 1s;
    transtion-timing-function: linear;
    
}
div.keyboard:hover{width: 300px; opacity: 1;}
```
  
## transition-delay 속성
* transition 효과가 나타나기 전까지 지연시간을 설정함

## transition과 transform 동시 적용
```css
.App-header .DeathPlayer {
    position: absolute;
    width: 50px;
    top: 50px;
    opacity: 0.2;
    transition: all 0.5s;
    transition-timing-function: ease;
}
.App-header .DeathPlayer:hover{
    opacity: 1;
    transform:matrix(1, 0, 0, 1, 0, 50);
}

```

# 애니메이션 
* CSS3에서는 애니메이션 효과를 사용하기 위해서 애니메이션에 대한 키프레임을 정의해야 함 
```css
.ani {
  animation-name: movingPara;
  animation-duration: 3s;
}

@keyframes movingPara{
  /* from에 처음 스타일, to에 마지막 스타일 정의 */
  from { margin-left: 100% }
  to { margin-left: 0% }
}

@keyframes DeathPlayerEffect{
  /* %를 사용해서 키프레임 적용 */
  0% {  opacity: 1; }
  50% { opacity: 1; transform:matrix(1, 0, 0, 1, 0, 50px); }
  100% { opacity: 0; top: 100px;}
}
```

## 애니메이션 키프레임 
* from(시작) => to (종료) 까지 설정할 수 있음
* 또는 %를 사용해서 키프레임을 설정할 수 있음

## animation-duration 속성
* 애니메이션의 재생시간을 정의합니다. 
* 기본 값은 0이며 재생 할 시간을 명시하지 않으면 아무 효과가 없습니다. 

## animation-delay 속성
* 애니메이션이 나타나기 까지 지연 시간을 설정합니다. 
  
## animation-iteration-count 속성
* 애니메이션의 반복횟수를 설정합니다. 
* 이 값을 infinite로 설정하면 무한히 반복됩니다. 
```css
* {
animation-iteration-count: 2;
}
```

## animation-direction 속성
* 애니메이션의 진행 방향을 설정합니다. 
* reverse : 반대방향으로 설정
* alternate : 정방향 -> 반대 방향 반복해서 동작 
```css
   div {
        animation-name: movingPara;
        animation-duration: 2s;
    }

    /* backward는 반대방향으로 동작 */
    #backward {
        animation-direction: reverse;
    }
```

## animation-timing-function 속성
* transition-timing-function 속성과 같음

## 애니메이션 축약 표현
```css
div{
  animation-name: hand;
  animation-duration: 3s;
  animation-timing-function: ease-in-out;
  animation-delay: 1s;
  animation-iteration-count: 3;
  animation-direction: alternate;
}

/* 위의 애니메이션을 축약해서 표현 */
div{ animation: hand 3s ease-in-out 1s 3 alternate}
```