# 導入手順

1. [Unity PR #1](https://github.com/Kig15/KosenChanbara/pull/1) の `sanuka` を `main` へマージします。
2. Docker Desktopを起動します。
3. このフォルダ内の2ファイルをUnityプロジェクト直下へ置き、そこでPowerShellを開いて実行します。

```powershell
docker load -i .\kosen-chanbara-phone-controller-amd64.tar.gz
docker run --rm --name kosen-controller -p 127.0.0.1:8080:8080 --mount "type=bind,source=$($PWD.Path)\Assets\StreamingAssets,target=/unity-config" kosen-chanbara-phone-controller:1.0.0
```

PCとスマホを同じWi-Fiへ接続し、Unityで `BattleGround` を再生してQRを読みます。停止は `Ctrl+C` です。
