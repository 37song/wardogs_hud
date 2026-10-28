# WARDOGS_PROFIT_HUD v1.0.0

[![Release](https://img.shields.io/github/v/release/37song/wardogs_hud?color=brightgreen)](https://github.com/37song/wardogs_hud/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Donate-yellow.svg)](https://buymeacoffee.com/37song)
[![KakaoPay 후원하기](https://img.shields.io/badge/KakaoPay-후원하기-FFCD00?style=flat&logo=kakaotalk&logoColor=black)](https://qr.kakaopay.com/Ej7jxWIW1)

스팀(Steam) 게임 **WARDOGS**의 실시간 손익($)을 게임 화면 위에 가볍고 깔끔한 오버레이로 띄워주는 공식 HUD 프로그램입니다.

---

## ✨ 주요 기능

1. **🌐 스팀 웹 채팅과 실시간 손익 동기화**
   - 프로그램 상단의 `[스팀 로그인]` 버튼을 통해 스팀에 로그인하면 실시간 손익이 HUD에 즉시 표시됩니다.
2. **🎯 가볍고 쾌적한 게임 오버레이 최적화**
   - 게임 화면 위에 항상 떠 있는 Always-On-Top 모드를 지원합니다.
   - 마우스 드래그로 원하는 위치로 자유롭게 이동 및 크기 조절이 가능합니다.
   - 게임 플레이에 방해되지 않는 투명 배경 및 클릭 통과(Click-through) 잠금 모드를 지원합니다.
3. **👤 간편한 계정 전환**
   - 부계정이나 다른 스팀 계정으로 전환하고 싶을 때는 `[스팀 로그인]` 버튼을 **마우스 우클릭**하여 손쉽게 새 계정으로 로그인할 수 있습니다.

---

## 🚀 빠른 시작 (사용 방법)

1. [최신 Releases](https://github.com/37song/wardogs_hud/releases)에서 `WARDOGS_PROFIT_HUD.zip`을 다운로드하고 압축을 풉니다.
2. `WARDOGS_PROFIT_HUD.exe`를 실행합니다.
3. 프로그램 상단의 **`[스팀 로그인]`** 버튼을 클릭합니다.
4. 열리는 창에서 본인의 Steam 계정으로 로그인합니다.
   - *(브라우저 상단에 "다른 앱에서 액세스 허용" 권한 팝업이 나타나면 **[허용]**을 클릭해 주세요.)*
5. 로그인이 완료되면 상단 상태바에 실시간 손익이 표시되며, 게임 화면의 오버레이로 즉시 확인할 수 있습니다!

> **팁**: 계정을 변경하고 싶을 때는 `[스팀 로그인]` 버튼을 **우클릭**하세요.

---

## 🛡️ 작동 방식 및 게임 성능 영향 (FAQ)

### 1. 게임 프로세스에 직접 접근하지 않음 (안티치트 100% 안전)
- 실제 게임 클라이언트(`WardogsClient-Win64-Shipping.exe`)의 메모리를 후킹하거나 코드를 변조하거나 DLL 주입(Injection)을 **일체 수행하지 않습니다.**
- 스팀이 관리하는 공식 스팀 웹 채팅의 공개 인게임 상태 데이터만을 로컬에서 안전하게 수신하므로, **안티치트 제재나 밴 위험으로부터 완전히 안전**합니다.

### 2. CPU 점유율 및 프레임(FPS) 영향: 0% 수준 (초경량 설계)
- **CPU 점유율 0.0% ~ 0.1%**: 웹 브라우저 탭 하나 띄운 것보다 가볍게 동작합니다.
- **메모리(RAM) 약 50MB**: 순수 C# 네이티브 윈도우 API 기반으로 설계되어 초경량 시스템 자원만 사용합니다.
- **프레임(FPS) 저하 없음**: 게임 엔진 내부 파이프라인에 간섭하지 않는 독립 투명 윈도우 오버레이 방식으로 작동하므로, 사양에 상관없이 인게임 프레임 드랍이 전혀 발생하지 않습니다.

---

## ☕ 후원하기 (Support & Donate)

본 프로그램이 유용하셨다면 개발자에게 따뜻한 커피 한 잔을 선물해주세요!  
보내주신 응원은 지속적인 업데이트와 프로그램 유지 관리에 큰 힘이 됩니다.

### 🌐 Buy Me a Coffee (해외 / 카드 결제)
[![Buy Me A Coffee](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://buymeacoffee.com/37song)

- **후원 링크**: https://buymeacoffee.com/37song

<br>

### 💛 카카오페이 후원 (국내 송금)

<img src="./.github/kakaopay_qr.png" width="160" alt="카카오페이 송금 QR코드" />

* **PC 사용자**: 스마트폰 카메라 앱 또는 카카오톡으로 위 **QR코드를 스캔**하시면 즉시 송금 화면이 열립니다.
* **스마트폰 사용자**: **[📱 카카오페이 송금 바로가기](https://qr.kakaopay.com/Ej7jxWIW1)** 터치 시 카카오톡 송금창으로 바로 이동합니다.

---

## 📄 라이선스
MIT License
