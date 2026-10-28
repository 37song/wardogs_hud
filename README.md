# WARDOGS_PROFIT_HUD v1.0.0

[![Release](https://img.shields.io/github/v/release/37song/wardogs_hud?color=brightgreen)](https://github.com/37song/wardogs_hud/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Donate-yellow.svg)](https://buymeacoffee.com/37song)
[![KakaoPay 후원하기](https://img.shields.io/badge/KakaoPay-후원하기-FFCD00?style=flat&logo=kakaotalk&logoColor=black)](https://qr.kakaopay.com/Ej7jxWIW1)

스팀(Steam) 게임 **WARDOGS**의 실시간 손익($)을 게임 화면 위에 가볍고 깔끔한 오버레이로 띄워주는 공식 HUD 프로그램입니다.

---

## ✨ 주요 기능

1. **스팀챗과 실시간 수익 동기화**
   - 스팀 로그인 후 친구 목록에 표시되는 실시간 매치 순손익을 HUD 오버레이로 즉시 동기화합니다.
2. **오버레이 최적화**
   - 게임 화면 위에 항상 표시되는 Always-On-Top 및 자유로운 위치 이동·크기 조절을 지원합니다.
   - 투명 배경 및 클릭 통과(Click-through) 잠금 모드로 게임 플레이에 방해 없이 쾌적하게 사용 가능합니다.
3. **간편한 계정 전환**
   - 부계정 등 다른 스팀 아이디로 로그인할 때는 `[스팀 로그인]` 버튼을 마우스 우클릭하여 손쉽게 계정을 변경할 수 있습니다.

---

## 🚀 빠른 시작 (사용 방법)

1. [최신 Releases](https://github.com/37song/wardogs_hud/releases)에서 `WARDOGS_PROFIT_HUD.zip`을 다운로드하고 압축을 풉니다.
2. `WARDOGS_PROFIT_HUD.exe`를 실행합니다.
3. 프로그램 상단의 **`[스팀 로그인]`** 버튼을 클릭합니다.
4. 열리는 창에서 본인의 Steam 계정으로 로그인합니다.
   - *(확장 프로그램의 백그라운드 서비스 워커 자동 연동으로 별도의 권한 팝업 허용 절차 없이 **로그인 즉시 자동 동기화**됩니다!)*
5. 로그인이 완료되면 상단 상태바에 실시간 손익이 표시되며, 게임 화면의 오버레이로 즉시 확인할 수 있습니다!

> ⚠️ **주의 (필독)**: 스팀 로그인 완료 후 열려 있는 **스팀 웹 채팅창(친구 목록 창)을 닫으시면 실시간 동기화가 중단**됩니다. 웹 창을 완전히 닫지 마시고 **최소화(`_`) 상태로 유지**해 주세요. (게임을 플레이하는 동안 백그라운드에서 가볍게 유지됩니다)

> 💡 **참고 (만약 브라우저에서 실수로 차단을 눌렀을 경우)**:  
> 프로그램 상단의 `[스팀 로그인]` 버튼을 **마우스 우클릭**하여 로그인 정보 및 권한을 초기화한 후, 다시 버튼을 클릭하여 새 창이 열리면 나타나는 팝업에서 **[허용]**을 눌러주시면 정상 동기화됩니다.

> 💡 **계정 변경**: 다른 아이디로 변경하고 싶을 때는 `[스팀 로그인]` 버튼을 **마우스 우클릭**하세요.

---

## 🛡️ 작동 방식 및 안전성

### 1. 스팀 공식 웹(Rich Presence) 안전 연동 방식
- 본 프로그램은 게임 클라이언트(`WardogsClient-Win64-Shipping.exe`)의 내부 메모리를 직접 스캔하거나 코드를 변조(Hooking), DLL 인젝션을 **일체 수행하지 않습니다.**
- 스팀 공식 서버에 동기화되는 친구 목록의 실시간 인게임 상태(Rich Presence: 내 캐릭터의 실시간 매치 순손익) 데이터를 전용 브라우저 창을 통해 읽어와 로컬 루프백(`127.0.0.1`) 통신으로 안전하게 오버레이 HUD에 표시합니다.
- 게임 파일을 전혀 건드리지 않는 순수 외부 오버레이 구조이므로 **안티치트 제재나 밴 위험이 0%**입니다.

### 2. 시스템 자원 및 인게임 성능 영향
- **일반 웹페이지 1개 수준의 최소 자원**: 스팀 웹 채팅 전용 경량 백그라운드 창 1개와 C# 네이티브 HUD만 구동되므로, 일반적인 인터넷 웹페이지 1개를 열어둔 정도의 미미한 리소스만 차지합니다.
- **인게임 프레임(FPS) 영향 없음**: 게임 엔진 내부 렌더링 파이프라인에 간섭하지 않고 독립적인 투명 윈도우 오버레이로 출력되므로, 저사양 PC에서도 게임 프레임 드랍이나 렉이 전혀 발생하지 않습니다.

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
