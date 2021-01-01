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

def CloseDBCon(dbcon):
    dbcon.close()

def GetSelectAllResult(dbcon, sql):
    cursor = dbcon.cursor()
    cursor.execute(sql)
    return cursor.fetchall()

def MakeJsonFileFromDic(sourceDic, jsonPath):
    f=open(jsonPath, 'w+' , encoding='utf-8')
    json_val = json.dumps(sourceDic,ensure_ascii=False )
    f.write(json_val)
    f.close()

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