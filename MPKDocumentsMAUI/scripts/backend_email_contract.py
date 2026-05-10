# Черновик для вашего FastAPI-бэкенда (не является частью сборки клиента).
# Реализуйте отправку через SMTP/sendgrid/aioSMTP + хранение кодов и pending-регистраций в Redis SQL.
#
# Новые роуты (согласование с клиентом MPKDocumentsMAUI.Shared):
#
# POST /auth/email/login/send        {"email": "..."}
# POST /auth/email/login/verify      {"email":"...","code":"..."} -> TokenResponse
# POST /auth/email/register/start    RegisterRequest поля snake_case как сейчас
# POST /auth/email/register/verify   {"email":"...","code":"..."} -> TokenResponse
#
# Письма о документах (нет в клиенте): при создании задачи получателю и при полном SIGNED отправителю —
# отправлять в тех же транзакциях/ивентах где меняются статусы в БД.
