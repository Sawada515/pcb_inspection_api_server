# メモ
## [must]
・エラーハンドリグ・例外が不十分

## 進捗
slot_status: OK
create_user: OK
delete_user: OK
user_list: OK

## curlコマンド　メモ
### create_user <= OK
curl -X POST -H "Content-Type: application/json" -d \
'{"username": "202521300", "password": "hogehgoe", "col": 2, "row": 2}' \
http://192.168.10.200:50000/create_user

### delete_user <= OK
curl -X POST -H "Content-Type: application/json" -d \
'{"username": "202521300"}' \
http://192.168.10.200:50000/delete_user

