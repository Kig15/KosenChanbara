# スマホコントローラー運用手順

P1/P2それぞれのQRをスマホで読み、端末の姿勢・角速度・重力除去済み加速度、ガード状態、リセンター要求をUnityへ送ります。入力データはWebRTC DataChannelで同一LAN内を直接流れます。外部サービスが扱うのはHTTPSの操作画面、QR生成、接続時のSDP/ICEシグナリングだけで、入力データは保存も中継もしません。STUN/TURNによるインターネット経由のフォールバックもありません。

Unityプロジェクトと接続サービスは別アプリとして配置しています。

```text
apps/
├── KosenChanbara/          # Unityゲーム
└── PhoneControllerSignal/ # VPS用の独立Webサービス
```

## 1. ローカルで起動する

PCとスマホを同じWi-Fiへ接続し、ワークスペース直下から次だけを実行します。所有ドメインもDockerも不要です。

```sh
make
```

ローカルNodeサーバー、一時HTTPSトンネル、Unity用 `controller-connection.json` がまとめて準備されます。表示された `https://...trycloudflare.com` がスマホ用の一時URLです。Unity自身は `ws://127.0.0.1:8080/signal` でローカルサーバーへ直結します。停止は `make stop`、状態確認は `make status` です。

```sh
make status
```

スマホのセンサーAPIには信頼済みHTTPSが必要なため、操作ページと接続時のシグナリングにだけCloudflare Quick Tunnelを使います。センサーデータ本体は同一LAN内のスマホからUnityへ直接流れます。一時URLは再起動すると変わり、Quick Tunnelは開発・動作確認向けです。固定URLが必要な展示本番では `make production DOMAIN=実ドメイン` のVPS構成を使います。

## 2. Unityを設定する

Makefileが `Assets/StreamingAssets/controller-connection.json` を自動生成します。手動設定する場合は次の形式です。

```json
{
  "signalingUrl": "ws://127.0.0.1:8080/signal",
  "hostKey": "自動生成されたHOST_KEY"
}
```

この実設定ファイルもgit管理対象外です。展示PCではファイルを置かず、Windowsの環境変数 `KOSEN_CONTROLLER_SIGNALING_URL` と `KOSEN_CONTROLLER_HOST_KEY` を使うこともできます。環境変数はUnityまたはビルド済みゲームを起動する前に設定してください。

Unity 6000.3.21f1でプロジェクトを開くと `com.unity.webrtc@3.0.0` が解決されます。Build Settingsでは `BattleGround` が先頭です。Windows向けにビルドし、Windows Defender Firewallの問い合わせではプライベートネットワーク上の通信を許可します。

## 3. 展示用Wi-Fiを設定する

- PCと2台のスマホを同じSSID/VLANへ接続する
- APの「クライアント分離」「プライバシーセパレーター」「ゲスト端末間通信禁止」を無効にする
- QRを開いて接続する間は、そのWi-Fiから一時HTTPS URLへインターネット接続できるようにする
- PCのネットワークプロファイルを「プライベート」にし、ゲームのUDP通信をファイアウォールで許可する
- VPNなど、ローカル端末間通信を遮断する設定を使わない

一度 `Connected (direct LAN)` になれば、HTTPSトンネルへの接続が一時的に失われても確立済みのコントローラーはLAN内で動き続けます。直接接続できないネットワークでは外部中継へ切り替えず、約8秒でタイムアウトします。

## 4. 当日の操作

1. ゲームを起動し、左のP1 QRと右のP2 QRを各スマホで読む
2. 各スマホで「センサーを使う」を押し、iPhoneではセンサー権限を許可する
3. 右手にスマホを持ち、カメラを上、充電口を下、画面側を左、背面側を右へ向けて静止させ、「リセンター」を押す（Unity画面の `RECENTER` でも同じ）
4. スマホを剣として動かし、大きな `ガード` ボタンを押している間だけ防御する
5. QRを無効化して交換したい場合は該当プレイヤーの `NEW QR` を押す

データが200ms以上届かない場合は、剣の姿勢を最後の値で固定し、ガードは強制解除されます。

## 5. 事前チェック

- iPhone SafariとAndroid Chromeの両方で5回ずつQR接続する
- P1/P2が入れ替わらず、別々のQRで同時接続できる
- 指定の持ち方で「リセンター」を押すと、その姿勢が動きの基準になり、ゲーム内の棒が真上を向く
- ガードを押したままスマホを振っても、ゲーム内の剣の姿勢更新が止まらない
- ガード中に指を離す、画面を隠す、ホームへ戻る、Wi-Fiを切る各操作でガードが解除される
- HTTPSトンネル停止後も、すでに接続済みのスマホ入力が継続する
- `NEW QR` 後は古いURLから再接続できない

サービス単体の開発・テスト手順は、同じワークスペースの `apps/PhoneControllerSignal/README.md` を参照してください。
