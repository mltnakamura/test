NTLM認証
	※マシン名決めておく必要があり(localhostsとかだめ）
	sudo vi /etc/sysconfig/network
		HOSTNAME=mymachine.corp.example.com
		→再起動
	

	
	
■hostsの編集
	・ADサーバと自分の名前サーバ名がフルネームで引けるようにする。
	sudo vi /etc/hosts
		127.0.0.1	MYMACHINE.CORP.EXAMPLE.COM	MYMACHINE
		192.168.1.XXX	MYMACHINE
		192.168.1.yyy	adserver.corp.example.com	adserver

■resolv.confの編集
	sudo vi /etc/resolv.conf
		search	corp.example.com
		nameserver 192.168.1.yyy
	※DHCPから自動取得の場合は再起動時に毎回書き換わるので要確認
	　

■samba3xのインストール
	・古いバージョンの削除してsamba3xをインストール
	sudo yum remove samba samba-common
	sudo yum install samba3x

■sambaの設定
	sudo vi /etc/samba/smb.conf 

	######################################################################
	[global]
	workgroup = CORP                    #ドメインの前の部分を大文字で
	realm = CORP.EXAMPLE.COM            #ドメイン名を大文字で
	password server = adserver.corp.example.com     #ADサーバの名前を小文字で
	netbios name = mymachine               #自分のマシン名
	security = ADS
	allow trusted domains = No
	obey pam restrictions = Yes
	idmap backend = rid:HOGE=10000-19999
	idmap uid = 10000-19999
	idmap gid = 10000-19999
	template homedir = /home/%U
	template shell = /bin/bash
	winbind separator = @
	winbind use default domain = Yes
	######################################################################

■ krb5の設定
	sudo vi /etc/krb5.conf
	######################################################################
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
	 forwardable = yes

	[realms]
	 CORP.EXAMPLE.COM = {
	 kdc = ADSERVER.CORP.EXAMPLE.COM:88
	 kdc = 192.168.1.yyy:88
	 default_domain = corp.example.com
	 admin_server = ADSERVER.CORP.EXAMPLE.COM:749
	 }

	[domain_realm]
	 .corp.example.com = CORP.EXAMPLE.COM
	 corp.example.com = CORP.EXAMPLE.COM
	######################################################################
	

■winbindの起動
	sudo /etc/init.d/winbind start
	sudo /sbin/chkconfig winbind on

■Windowsドメインへの参加

	sudo net ads join -U administrator
	Enter administrator's password:

	※うまくいかない場合はいままでの設定を見直す
	/etc/hosts
	/etc/resolv.conf
	/etc/samba/smb.conf
	/etc/krb5.conf
	
	Host is not configured as a member server.
	Invalid configuration.  Exiting....
	特に上記の場合、smb.confのsecurity = ADSを確認
	記述以降にsecurity = usersなどある場合はコメントまたは削除する
	
	・ドメイン参加には成功するがDNSへの登録に失敗する場合
	No DNS domain configured for MYMACHINE. Unable to perform DNS Update.
	/etc/hostsの127.0.0.1にFQDNを登録する。
	127.0.0.1	MYMACHINE.CORP.EXAMPLE.COM
	DNSに登録できればNEURONのアドレスがIPでなく、マシン名でもOKになる。
		
DNS update failed!


■参加の確認方法（任意）
	sudo net ads info
	sudo net ads testjoin
	sudo ntlm_auth --username user1
	
	sudo ntlm_auth --username=DOMAIN\\username

	※うまくいかない場合はwinbindの起動を確認（再起動）
	sudo /etc/init.d/winbind restart

■mod_auth_ntlm_winbindのインストール

	・httpd-develのインストール
	sudo yum httpd-devel

	・mod_auth_ntlm_winbindの取得
	cd
	svn co svn://svnanon.samba.org/lorikeet/trunk/mod_auth_ntlm_winbind mod_auth_ntlm_winbind
	cd mod_auth_ntlm_winbind/
	sudo autoconf
	sudo ./configure --with-apxs=/usr/sbin/apxs --with-apache=/usr/sbin/httpd
	sudo make
	sudo make install

	・/var/lib/samba/winbindd_privileged のグループ権限をapache にする。
	sudo chown root.apache /var/lib/samba/winbindd_privileged
	sudo chown root.apache /usr/bin/ntlm_auth

■Apache設定ファイルの編集
	
	sudo vi /etc/httpd/conf/httpd.conf

	最後に追加
	#########################################################################	
	KeepAlive On
	LoadModule auth_ntlm_winbind_module modules/mod_auth_ntlm_winbind.so

	<Location />
	  NTLMAuth on
	  AuthType NTLM
	  AuthName "Neuron Login"
	  NTLMAuthHelper "/usr/bin/ntlm_auth --helper-protocol=squid-2.5-ntlmssp"
	  NTLMBasicAuthoritative on
	  require valid-user
	  
	#  Cookie Setting
	  RewriteEngine on
	  RewriteCond %{REMOTE_USER} (.*)
	  RewriteRule .* - [E=REMOTE_USER:%1]
	  Header add Set-Cookie "uid=%{REMOTE_USER}e@corp.example.com; path=/; expires=Thu,1-jan-2030 00:00:00 GMT"	  
	</Location>
	#########################################################################	
	※Set-Cookieの{REMOTE_USER}eの後ろの@以降は認証したADサーバのドメイン
	
■SELinuxの無効化

	・現在のモード確認　→　Enforcingだと認証がうまくいかない
	　Permission denied: couldn't spawn child ntlm helper process: /usr/bin/ntlm_auth
	sudo /usr/sbin/getenforce
	
	・現在のモードをpermissiveにする
	sudo /usr/sbin/setenforce 0
	
	・起動時のモードをpermissiveにする
	sudo  vi /etc/selinux/config
		SELINUX=permissive
	
■Apacheの再起動

	sudo /sbin/service httpd restart

