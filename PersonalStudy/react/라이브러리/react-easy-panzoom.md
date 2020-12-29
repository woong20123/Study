# react-easy-panzoom
* react-panzoom은 react 16버전 이상이 필요합니다. 
## 용법
### Key mapping
* PanZoom 컴포넌트는 기본적으로 화살표 키 및 키보드와 상화작용을 지원합니다 
### Prevent pan
* 때때로 뷰가 이동하는 것을 방지하는 것이 유용할 수 있습니다. 
* 예를 들어서 클릭 가능한 요소에서 팬 시작이 수행되는 경우입니다.
* Panzoom은 패닝을 방지하는 기능을 정의 할 수 있는 preventPan소품을 제공합니다. 

### Boundaries
* `PanZoom` 패닝을 제한하기 위해서 enableBoundingBox를 지원합니다. 
* box는 내부 콘텐츠의 너비와 높이로 계산됩니다. 
* 경계 상자가 내부 콘텐츠의 특정 비율까지 이동할 수 있도록 비율이 적용 됩니다. 
* 기본적으로 비율은 0.8이지만 수정 할 수 있습니다. 
* 이 경우 상위 컨테이너 외부에서 최대 80%까지 팬할 수 있습니다. (남은 20%는 항상 표시됩니다. )

### 속성
|이름|타입|기본값|설명|
|:--|:--|:--|:--|
|autoCenter|bool|false|마운트할 댸 뷰를 자동으로<br> 중앙으로 설정|
|autoCenterZoomLevel|number||자동 중앙에 대한 초기<br> 확대/축소 레벨 지정|
|zoomSpeed|number|1|zoom 속도 지정|
|doubleZoomSpeed|number|1.75|더블 클릭의 zoom 속도를 설정|
|disabled|bool|false| 이동 및 확대/축소 비활성화|