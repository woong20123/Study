import csv
import LibMysql
import json


# 아이템 전용 Insert 
itemSql = """
INSERT INTO `pbn`.`item_data` (`seq`,`time`,`name`)
VALUES (%s,%s,%s)
"""
dbcon = WSMysql.GetDBCon('root','test','test','test')

# mysql에 csv데이터를 insert합니다.
# WSMysql.InsertMysqlFromCSV(dbcon,itemSql, 
# csvPath='C://Users//woong20123//Documents//GitHub//Study//Python//Mysql에 데이터넣기//Data//아이템.csv', 
# columCount=25)

select_actor_id_lists = '''(1111,
2222)
'''

# 포지션 정보 추가
selectSqlPosition='''
SELECT b.actor_name, a.pday_minutes, a.actor_location, total_exp, total_money, zone_code, zone_name
from personal_strava_record as a
join basic_data as b 
on a.actor_id = b.actor_id
where a.actor_id in %s
order by a.actor_id
''' % select_actor_id_lists

baseDic = {}
curs=WSMysql.GetSelectAllResult(dbcon, selectSqlPosition)
for row in curs:
    pos = row[2].split('/')
    key = row[0]+'_'+str(row[1])
    baseDic[key] ={"name":row[0],"time":row[1],"x":pos[0], "y":pos[1],"exp":row[3],"adena":row[4], "zone_code":row[5], "zone_name":row[6],"event":[]}

# 최종 결과 저장
resultData={}
for k, v in baseDic.items():
    keys = k.split('_')
    if keys[0] not in resultData.keys() :
        resultData[keys[0]] = []
    resultData[keys[0]].append(v)
WSMysql.MakeJsonFileFromDic(resultData,"C://Users//woong20123//Documents//GitHub//Study//Python//Mysql에 데이터넣기//Data//Result.json")

WSMysql.CloseDBCon(dbcon)
