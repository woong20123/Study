import pymysql
import pandas as pd
import csv
from datetime import datetime
import json

## DB Connection을 받아온다.
def GetDBCon(inputUser, password, host, db):
    dbcon = pymysql.connect(
    user=inputUser,
    passwd=password,
    host=host,
    db=db,
    charset='utf8')
    return dbcon

# CSV파일을 Mysql에 insert 합니다.
def InsertMysqlFromCSV(dbcon, sql, csvPath, columCount):
    f=open(csvPath,'r' , encoding='utf-8')
    csvReader=csv.reader(f)

    lineCount = 0
    for row in csvReader :
        if(lineCount > 0):
            imputParam = []

            # 컬럼을 만듭니다.
            for i in range(0, columCount):
                imputParam.append(row[i])

            cursor = dbcon.cursor()
            cursor.execute(sql,imputParam)
        lineCount+=1

    dbcon.commit()

# Mysql에서 데이터를 select 해서 csv파일로 리턴합니다. 
def BaseSelectMysqlToJson(dbcon, sql, jsonPath): 
    cursor = dbcon.cursor()
    cursor.execute(sql)
    i = 0
    result_dic = {}
    curs = cursor.fetchall()
    for row in curs:
        pos = row[2].split('/')
        key = row[0]+'_'+str(row[1])
        result_dic[key] ={"name":row[0],"time":row[1],"x":pos[0], "y":pos[1],"exp":row[3],"adena":row[4],"event":[]}
        i = i + 1

    f=open(jsonPath, 'w+' , encoding='utf-8')
    json_val = json.dumps(result_dic,ensure_ascii=False )
    f.write(json_val)
    f.close()
    
    return result_dic

 # Mysql에서 데이터를 select 해서 csv파일로 리턴합니다. 
def ItemSelectMysqlToJson(baseDic, dbcon, sql, jsonPath): 
    cursor = dbcon.cursor()
    cursor.execute(sql)
    i = 0
    result_dic = []
    curs = cursor.fetchall()
    for row in curs:
        dt = row[1]
        time = (int(dt.hour) * 60 + int(dt.minute)) + 1
        key = row[0]+'_'+str(time)
        value=baseDic[key]
        value["event"].append({"time":time,"type":"getItem", "name":row[2],"text":row[2]})
        result_dic.append({"time":time,"type":"getItem", "name":row[2],"text":row[2]})
        i = i + 1

    f=open(jsonPath, 'w+' , encoding='utf-8')
    json_val = json.dumps(baseDic,ensure_ascii=False )
    f.write(json_val)
    f.close()
    
    return result_dic

def DeathSelectMysqlToJson(baseDic, dbcon, sql, jsonPath): 
    cursor = dbcon.cursor()
    cursor.execute(sql)
    i = 0
    result_dic = []
    curs = cursor.fetchall()
    for row in curs:
        dt = row[1]
        time = (int(dt.hour) * 60 + int(dt.minute)) + 1
        key = row[0]+'_'+str(time)
        value=baseDic[key]
        value["event"].append({"time":time,"type":"death", "name":row[2],"text":row[2]})
        result_dic.append({"time":time,"type":"death", "name":row[2],"text":row[2]})
        i = i + 1

    f=open(jsonPath, 'w+' , encoding='utf-8')
    json_val = json.dumps(baseDic,ensure_ascii=False )
    f.write(json_val)
    f.close()
    
    return result_dic

def LevelUpSelectMysqlToJson(baseDic, dbcon, sql, jsonPath): 
    cursor = dbcon.cursor()
    cursor.execute(sql)
    i = 0
    result_dic = []
    curs = cursor.fetchall()
    for row in curs:
        dt = row[1]
        time = (int(dt.hour) * 60 + int(dt.minute)) + 1
        key = row[0]+'_'+str(time)
        value=baseDic[key]
        text = '124일 소요'
        value["event"].append({"time":time,"type":"levelup", "name":row[2],"text":text})
        result_dic.append({"time":time,"type":"levelup", "name":row[2],"text":text})
        i = i + 1

    f=open(jsonPath, 'w+' , encoding='utf-8')
    json_val = json.dumps(baseDic,ensure_ascii=False )
    f.write(json_val)
    f.close()
    
    return result_dic

def PvPSelectMysqlToJson(baseDic, dbcon, sql, jsonPath): 
    cursor = dbcon.cursor()
    cursor.execute(sql)
    i = 0
    result_dic = []
    curs = cursor.fetchall()
    for row in curs:
        dt = row[1]
        time = (int(dt.hour) * 60 + int(dt.minute)) + 1
        key = row[0]+'_'+str(time)
        value=baseDic[key]
        value["event"].append({"time":time,"type":"pvp", "name":row[3],"text":row[4]})
        result_dic.append({"time":time,"type":"pvp", "name":row[3],"text":row[4]})
        i = i + 1

    f=open(jsonPath, 'w+' , encoding='utf-8')
    json_val = json.dumps(baseDic,ensure_ascii=False )
    f.write(json_val)
    f.close()
    
    return result_dic
