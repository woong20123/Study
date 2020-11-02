import csv
import WSMysql
import json

# raw 전용 Insert
rawSql = """
INSERT INTO `pbn`.`death_data` (`Seq`,`Time`,`LogID`,`LogName`,`Log_Detail_Code`,`Actor_Code`,`Actor_Account_ID`,`Actor_World`,`Actor_ID`,`Actor_Name`,`Actor_Level`,`Actor_Born`,`Actor_Class`,`Actor_Party`,`Actor_Guild`,`Actor_MAP_ID`,`Actor_MAP_OID`,`Actor_Zone_ID`,`Actor_Location`,`Actor_Num1`,`Actor_Num2`,`Actor_Num3`,`Actor_Str1`,`Actor_Str2`,`Actor_Str3`,`Entity_Code`,`Entity_ID`,`Entity_DBID`,`Entity_Grade`,`Entity_Lv`,`Entity_Option`,`Entity_Str1`,`Entity_Str2`,`Old_Num1`,`Old_Num2`,`Old_Str1`,`Use_Num1`,`Use_Num2`,`New_Num1`,`New_Num2`,`New_Str1`,`Data_Num1`,`Data_Num2`,`Data_Num3`,`Data_Num4`,`Data_Num5`,`Data_Str1`,`Data_Str2`,`Data_Str3`,`Target_Code`,`Target_Account_ID`,`Target_World`,`Target_ID`,`Target_Name`,`Target_Level`,`Target_Born`,`Target_Class`,`Target_Party`,`Target_Guild`,`Target_MAP_ID`,`Target_MAP_OID`,`Target_Zone_ID`,`Target_Location`,`Target_Num1`,`Target_Num2`,`Target_Num3`,`Target_Str1`,`Target_Str2`,`Target_Str3`,`plogid`,`plogdate`)
VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,
%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,
%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,
%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,
%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,
%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,
%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,%s)
"""

# 아이템 전용 Insert 
itemSql = """
INSERT INTO `pbn`.`item_data` (`seq`,`time`,`actor_account_id`,`actor_world`,`actor_id`,`actor_name`,`actor_born`,`actor_class`,`actor_party`,`actor_guild`,`actor_map_id`,`actor_map_oid`,`actor_zone_id`,`actor_location`,`actor_str2`,`entity_id`,`entity_dbid`,`entity_grade`,`entity_lv`,`entity_option`,`use_num1`,`new_num1`,`target_account_id`,`target_id`,`item_nm`)
VALUES (%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,
%s,%s,%s,%s,%s,%s,%s,%s,%s,%s,
%s,%s,%s,%s,%s)
"""

dbcon = WSMysql.GetDBCon('root','pbn!2345','172.19.153.153','pbn')

# mysql에 csv데이터를 insert합니다.
# WSMysql.InsertMysqlFromCSV(dbcon,itemSql, 
# csvPath='C://Users//woong20123//Documents//GitHub//Study//Python//Mysql에 데이터넣기//Data//아이템.csv', 
# columCount=25)

select_actor_id_lists = '''(37436171902517251,
37436171902522464,
37436171902524788,
37436171902552647,
37436171902553180,
37436171902555130,
37436171902534928,
37436171902527698,
37436171902528058,
37436171902530557,
37436171902517396,
37436171902527698,
37999121855947633,
37436171902534611,
37436171902552119,
37436171902533080)
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

# 아이템 정보 추가
selectSqlItem='''
SELECT b.actor_name, a.time, a.item_nm
from pbn.item_data as a
join pbn.basic_data as b 
on a.actor_id = b.actor_id
where a.actor_id in %s
order by a.actor_id
''' % select_actor_id_lists
curs=WSMysql.GetSelectAllResult(dbcon, selectSqlItem)
for row in curs:
    dt = row[1]
    time = (int(dt.hour) * 60 + int(dt.minute)) + 1
    key = row[0]+'_'+str(time)
    value=baseDic[key]
    value["event"].append({"time":time,"type":"getItem", "name":row[2],"text":row[2]})

# Death 정보
selectSqlDeath='''
SELECT b.actor_name, a.time, a.Target_Name
from pbn.death_data as a
join pbn.basic_data as b 
on a.actor_id = b.actor_id
where a.actor_id in %s
order by a.actor_id
''' % select_actor_id_lists
curs=WSMysql.GetSelectAllResult(dbcon, selectSqlDeath)
for row in curs:
    dt = row[1]
    time = (int(dt.hour) * 60 + int(dt.minute)) + 1
    key = row[0]+'_'+str(time)
    value=baseDic[key]
    value["event"].append({"time":time,"type":"death", "name":row[2],"text":row[2]})

# 레벨업 정보
selectSqlLevelup='''
SELECT b.actor_name, a.time, a.actor_level
from pbn.level_data as a
join pbn.basic_data as b 
on a.actor_id = b.actor_id
where a.actor_id in %s
order by a.actor_id
''' % select_actor_id_lists
curs=WSMysql.GetSelectAllResult(dbcon, selectSqlLevelup)
for row in curs:
    dt = row[1]
    time = (int(dt.hour) * 60 + int(dt.minute)) + 1
    key = row[0]+'_'+str(time)
    value=baseDic[key]
    text = '124일 소요'
    value["event"].append({"time":time,"type":"levelup", "name":row[2],"text":text})
        
# pvp 정보
selectSqlPVP='''
SELECT b.actor_name, a.time, a.Actor_str2, a.Target_Name, a.Target_str2
from pbn.pvp_data as a
join pbn.basic_data as b 
on a.actor_id = b.actor_id
where a.actor_id in %s 
order by a.actor_id
''' % select_actor_id_lists
curs=WSMysql.GetSelectAllResult(dbcon, selectSqlPVP)
for row in curs:
    dt = row[1]
    time = (int(dt.hour) * 60 + int(dt.minute)) + 1
    key = row[0]+'_'+str(time)
    value=baseDic[key]
    value["event"].append({"time":time,"type":"pvp", "name":row[3],"text":row[4]})

# 최종 결과 저장
resultData={}
for k, v in baseDic.items():
    keys = k.split('_')
    if keys[0] not in resultData.keys() :
        resultData[keys[0]] = []
    resultData[keys[0]].append(v)
WSMysql.MakeJsonFileFromDic(resultData,"C://Users//woong20123//Documents//GitHub//Study//Python//Mysql에 데이터넣기//Data//Result.json")

WSMysql.CloseDBCon(dbcon)
