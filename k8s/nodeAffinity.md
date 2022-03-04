## 노드 어피니티 
* 노드 어피니티 설정을 통해서 특정 노드에 pod을 할당 할 수 있습니다.
```bash
# 레이블을 나열 할 수 있습니다. 
kubectl get nodes --show-labels

# 노드에 레이블을 추가 할 수 있습니다. 
kubectl label nodes <node-name> disktype=ssd
```

### 노드 어피니티로 pod 스케줄링
* 스케줄링 방법은 두가지가 있음
* requiredDuringSchedulingIgnoredDuringExecution
  * 해당 조건을 무조건 만족해야 합니다.
* preferredDuringSchedulingIgnoredDuringExecution
  * 우선 순위를 배정해서 배포 가능 

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: nginx
spec:
  affinity:
    nodeAffinity:
      requiredDuringSchedulingIgnoredDuringExecution:
        nodeSelectorTerms:
        - matchExpressions:
          - key: disktype
            operator: In
            values:
            - ssd            
```

## 참조 
* https://kubernetes.io/ko/docs/tasks/configure-pod-container/assign-pods-nodes-using-node-affinity/