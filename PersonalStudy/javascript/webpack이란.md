* https://d2.naver.com/helloworld/0239818

# webpack이 필요한 이유
* 자바스크립트는 언어 자체가 지원하는 모듈 시스템이 없음
* 이러한 한계를 극복하기 위한 방법중에 하나가 webpack임
* 자바스크립트 모듈화 명세를 만드는 CommonJS와 AMD의 명세를 모두 지원함
  * https://d2.naver.com/helloworld/12864
# 자바스크립트에서 모듈 정의와 모듈 사용
```js
// 외부에 배포할 모듈을 정의합니다. 
module.exports = { message : 'webpack'};

// examplemodule.js 파일의 모듈을 사용하기
alert(require('./examplemodule').message);
```
* 모듈을 정의하고 사용하게 로딩하는 방법은 어렵지 않지만 webpack으로 컴파일해서 브라우져에서 실행할 수 있는 형태로 바꿔야 한다. 

# webpack 사용방법 
* webpack이 설치되면 다음과 같이 명령어를 실행해서 모듈을 컴파일 합니다. 
```js
// webpack {엔트리 파일 경로} {번들 파일 경로}
webpack ./entry.js bundle.js
```
* 엔트리 파일이란 서로 의존 관계에 있는 다양한 모듈을 사용하는 시작점이 되는 파일
* 번들 파일은 브라우저에서 실행 할 수 있도록 모듈을 컴파일한 파일
* 자바 스크립트를 사용하는 HTML 코드에서는 컴파일 결과로 만들어진 번들 파일만 포함하면 된다. 

```js
// webpack 명령어에 --watch 옵션을 사용하면 모듈 파일 변경될 때 마다 변경된 모듈을 자동으로 다시 컴파일 합니다. 
webpack --watch ./entry.js bundle.js  
```

# 모듈의 스코프 
* 컴파일 과정에서 각 모듈은 함수로 감싸진다. 
* webpack으로 모듈이 컴파일 된 뒤에는 지역 변수가 된다.

# 설정 파일 사용
* CLI로 webpack을 실행해 컴파일 할 때 엔트리 파일 많거나 옵션이 많으면 설정 파일을 만들어서 관리합니다. 
* 아래과 같은 형태로 `webpack.config.js` 파일을 작성해서 저장합니다. 
* https://webpack.js.org/configuration/ 에서 설정 파일을 확인 할 수 있음
```js
// webpack 설정파일의 기본 형태
module.exports = {
    context: __dirname + '/app', // 모듈 파일 폴더
    entry: {    // 엔트리 파일 목록
        app: `./app.js`
    },
    output: {
        path: __dirname + '/dist', // 번들 파일 폴더
        filename: '[name].bundle.js' // 번들 파일 이름 규칙
    }
}

// 디렉터리에서 다음과 같이 간단하게 명령어를 입력하면 컴파일을 시작합니다. 
webpack
```

# webpack 컨셉 번역하기 
* https://webpack.js.org/concepts/

# 로더 
* webpack의 로더는 다양한 리소스를 JavaScript에서 바로 사용할 수 있는 형태로 로딩하는 기능
* 다음 그림과 같이 로더의 종류에 따라서 javaScript에서 다양한 결과를 얻을 수 있음
  * json-loader => 데이터 객체
  * handlebars-loaders => 템플릿 함수
  * coffescript => javaScript
* 예를 들어 템플릿 라이브러리인 HandleBars를 로딩하는 로더인 Handlebars-loader를 사용하기
```js
// 설치하기 
npm install handlebars-loader


// 설정 파일 
module.exports = {
    ...
    output : {
        ...
    },
    module : [
        loaders : {
            // 적용할 파일의 패턴과 적용할 로더 적용
            {
                test : /\-tps.html$/,
                loader : 'handlebars'
            }     
        }]
    }
}

/// example-tpl.html
<div>{{greeting}}</div>

/// example-tpl.html 로드하는 js파일
var listTpl = require('./example-tpl.html');
listTpl( { greeting: 'Hello World'} );
```

# 개발자 도구 연동
* webpack의 경우 컴파일이후에 엔트리 파일 갯수에 따라서 모듈들이 병합됨 
* 파일 구조를 파악해서 디버깅할 때 어려움이 있음
* webpack.config.js 파일에 아래와 같이 추가해서 컴파일하면 파일 구조 확인 가능
```js
module.exports = {
    ...
    devtool : '#inline-source-map'
}
```

# 빌드 도구 연동
* grunt-webpack
* gulp-webpack