

■AD側環境設定
特になし

■Apache側環境設定
---------------------------------------------------------------
mod_auth_kerbインストール（ケルベロス認証用モジュール）
---------------------------------------------------------------

 centosの場合
	sudo yum install mod_auth_kerb

 ubuntuの場合
	apt-get install libapache2-mod-auth-kerb

---------------------------------------------------------------
Apache　サーバ　/etc/krb5.conf　の内容を編集
---------------------------------------------------------------
[logging]
 default = FILE:/var/log/krb5libs.log
 kdc = FILE:/var/log/krb5kdc.log
 admin_server = FILE:/var/log/kadmind.log

[libdefaults]
 default_realm = CORP.EXAMPLE.COM
 dns_lookup_realm = false
 dns_lookup_kdc = false
 ticket_lifetime = 60m
 renew_lifetime = 1d
 forwardable = true

[realms]
 CORP.EXAMPLE.COM = {
　## ↓認証ADサーバ（IPアドレス）を指定（port:88）
  kdc = 192.168.1.201:88
 }

[domain_realm]
 .corp.example.com = CORP.EXAMPLE.COM
 corp.example.com = CORP.EXAMPLE.COM


---------------------------------------------------------------
Apache　サーバ　/etc/httpd/conf.d/auth_kerb.conf　の内容を編集
---------------------------------------------------------------
<Location />
#  Kerberos Setting
  AuthType Kerberos
  AuthName "Kerberos Login"
  ## ↓統合Windows認証の場合Webサイトを登録（keytabを作った時のprincの＠より前と一致させる必要がある）
  KrbServiceName HTTP/neuron-server

  KrbMethodNegotiate Off					## ←　統合認証　Off
  KrbMethodK5Passwd On					## ←　Basic認証　On
  KrbAuthRealms CORP.EXAMPLE.COM
  KrbVerifyKDC   Off
  require valid-user
#  SSLRequireSSL
#  Krb5KeyTab /etc/httpd/conf/kerbneuron.keytab

#  Cookie Setting
  RewriteEngine on
  RewriteCond %{REMOTE_USER} (.*)
  RewriteRule .* - [E=REMOTE_USER:%1]
  Header add Set-Cookie "uid=%{REMOTE_USER}e; path=/"
</Location>

---------------------------------------------------------------
Apache再起動
---------------------------------------------------------------
	sudo service httpd restart


■クライアントPCブラウザ環境設定
-----------------------
IEの場合
-----------------------
□イントラサイト追加
	インターネットオプション
		↓
	セキュリティタブ
		↓
	ローカルイントラネット
		↓
	サイト追加（http://192.168.1.201）
	（認証を受けさせるWebサイトを登録しないとユーザー/パスワード入力ダイアログが別に表示される）

