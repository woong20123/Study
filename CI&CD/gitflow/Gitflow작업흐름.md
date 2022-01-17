## 작동 원리

### Develop 과 main 브런치들
* 단일 main 브런치 대신 이 워크 플로우는 두개의 브랜치를 사용하여 프로젝트의 히스토리를 관리합니다. 
* main 브랜치는 공식 릴리스 히스토리를 저장합니다.
* Develop 브랜치는 Feature들에 대한 통합 브랜치 역활을 합니다.
* 버전 번호로 메인 브랜치의 모든 커밋에 대한 태그를 지정하는 것도 편리합니다. 
* 첫번째 단계는 기본 main 브랜치를 develop브런치로 분리하는 것입니다.
  * 이를 수행하는 간단한 방법은 로컬 환경에서 비어있는 develop 브랜치를 만들고 서버에 push하는 것입니다.
```bash
git branch develop
git push -u origin develop
```
* develop 브랜치에는 프로젝트이 전체 기록이 포함되며, main에는 요약된 버전이 포함됩니다. 
* 다른 개발자는 중앙 저장소를 복제하여 develop을 위한 추적 브랜치를 만들어야 합니다. 
* git-flow 확장 라이브러리를 사용할 때 기존 repo에서 git flow init를 실행하면 develop분기가 생성됩니다.
  
### Feature 브랜치
* 각각의 새로운 feature들은 백업/협업을 위해서 중앙 저장소로 push될 수 있는 개별 브랜치로 있어야 합니다.
* Main 브랜치에서 분기하지 않고 develop 브랜치에서 분기합니다. 
* feature가 완료되면 develop으로 다시 병합됩니다.
* feature는 main과 직접 연결되지 않습니다. 
* feature 브랜치와 결합된 develop 브랜치의 목적과 의도는 Feature 브랜치의 워크플로입니다.
* feature 브랜치는 일반적으로 최신의 develop 브랜치로 생성됩니다. 

### Release 브랜치
* develop이 release에 대한 충분한 feature들이 구현되면 develop에서 release 브랜치를 생성합니다.
* 이 브랜치를 생성하면 다음 release 주기가 시작되므로 이지점 이후로는 새로운 기능을 추가 할 수 없습니다.
  * 이 브랜치에는 버그 수정, 문서 생성, 기타 Release용 작업만 포함되어야 합니다.
* 배포 준비가 완료되면 Release 브랜치가 Main에 Mergy되고 버전 번호가 태그로 지정됩니다. 
  * 또한 Develop에도 다시 병합되어야 합니다.
* Release를 위한 전용 branch를 사용하면 한 팀이 현재 Release를 수정하는 동안 다른팀이 다음 Release의 기능을 계속 작업할 수 있습니다.
  * 잘 정의된 개발 단계를 가집니다.
* Release가 출시될 준비가 되면 main과 develop에 mergy되고 release branch가 삭제됩니다.

### Hotfix 브랜치
* maintenance 또는 hotfix 브랜치를 사용해서 빠르게 live에 적용합니다. 
* hotfix는 develop 대신에 master를 기반으로 한다는 점 빼고는 release와 feature 브랜치와 매우 유사합니다. 
* master에서 직접 생성되는 유일한 브랜치 입니다.
* 수정이 완료되자마자 main과 develop에 둘 다 mergy됩니다.
* master는 업데이트 된 version 번호로 태그를 지정합니다. 
* 버그 수정을 위한 전담 개발 라인이 있으면 나머지 워크 플로를 중단하거나 다음 release 주기를 기다리지 않고 문제를 해결할 수 있습니다.

```bash
-- git flow 없이 수행
git checkout develop
git checkout -b release/0.1.0

git checkout main
git merge release/0.1.0

-- git flow로 실행
$ git flow release start 0.1.0
Switched to a new branch 'release/0.1.0'

git flow release finish '0.1.0'
```

## 참고
* https://www.atlassian.com/git/tutorials/comparing-workflows/gitflow-workflow