# WARDOGS 수익 HUD (Profit HUD) v2.0

[![Release](https://img.shields.io/github/v/release/37song/wardogs_hud?color=brightgreen)](https://github.com/37song/wardogs_hud/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Buy Me A Coffee](https://img.shields.io/badge/Buy%20Me%20A%20Coffee-Donate-yellow.svg)](https://buymeacoffee.com/37song)

스팀(Steam) 게임 **WARDOGS**의 실시간 손익($)을 게임 화면 위에 가볍고 깔끔한 오버레이로 띄워주는 HUD 프로그램입니다.

v2.0부터는 복잡한 개발자 도구나 북마크 설정 없이, **앱 내 원클릭 스팀 로그인**만으로 즉시 실시간 손익이 감지 및 연동됩니다.

---

## 🌟 주요 기능 및 v2.0 개선 사항

1. **🌐 원클릭 스팀 내장 연동 (초간편 연동)**
   - 더 이상 브라우저 콘솔창(F12)에 코드를 복사하거나 북마크를 누를 필요가 없습니다.
   - 프로그램 우측 상단의 [🌐 스팀 로그인] 버튼을 누르면 나타나는 로그인 창에서 Steam 1회 로그인만 완료하면 실시간 손익이 HUD에 즉시 표시됩니다.
2. **🛡️ 밸브(Valve) 공식 CSP & PNA 완벽 대응**
   - Steam Web Chat의 Content Security Policy(CSP) 규격을 준수하는 공식 로컬 통신 포트(127.0.0.1:27060) 및 Private Network Access(PNA) 헤더를 탑재하여 외부 유출 없는 100% 로컬 보안 통신을 보장합니다.
3. **🔄 세션 자동 복구 (Keep-Alive 와치독)**
   - 동기화 브라우저 창을 실수로 닫아도 프로그램 내부의 감시 스레드가 자동으로 최소화 상태로 복구하여 게임 중 손익 끊김이 발생하지 않습니다.
4. **👤 간편한 계정 전환 (우클릭)**
   - 부계정이나 다른 스팀 계정으로 전환하고 싶을 때는 [🌐 스팀 로그인] 버튼을 **마우스 우클릭**하면 기존 세션이 초기화되어 손쉽게 다시 로그인할 수 있습니다.
5. **🎯 게임 오버레이 최적화**
   - 게임 창 위에 항상 떠 있는 Always-On-Top 모드.
   - 드래그 앤 드롭으로 원하는 위치로 자유로운 이동.
   - 창 크기 조절 및 글자 크기 자동 반응형 스케일링.
   - 배경 투명화 및 클릭 통과(Click-through) 잠금 모드 지원.

---

## 🚀 빠른 시작 (사용 방법)

1. [최신 Releases](https://github.com/37song/wardogs_hud/releases)에서 WARDOGS_PROFIT_HUD.zip을 다운로드하고 압축을 풉니다.
2. WARDOGS 수익 HUD.exe를 실행합니다.
3. 프로그램 상단의 **[🌐 스팀 로그인]** 버튼을 클릭합니다.
4. 열리는 창에서 본인의 Steam 계정으로 로그인합니다.
5. 로그인이 완료되면 상단 상태바에 실시간 손익이 표시되며, 게임 내에서도 오버레이로 즉시 확인할 수 있습니다!

> **팁**: 계정을 변경하고 싶을 때는 [🌐 스팀 로그인] 버튼을 **우클릭**하세요.

---

## ⌨️ 단축키 안내

| 단축키 | 기능 |
|---|---|
| **F9** | 오버레이 숨기기 / 보이기 토글 |
| **F10** | 클릭 통과(Click-through) 고정 모드 토글 (마우스 클릭이 게임으로 바로 전달됨) |
| **마우스 드래그** | HUD 창을 클릭하여 화면 어디로든 이동 |
| **우하단 모서리 드래그** | HUD 창 크기 자유 조절 |

---

## ☕ 후원하기 (Buy Me a Coffee)

이 프로젝트가 마음에 드셨다면 개발자에게 따뜻한 커피 한 잔을 선물해주세요!

[![Buy Me A Coffee](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://buymeacoffee.com/37song)

- **후원 링크**: https://buymeacoffee.com/37song

---

## 📄 라이선스
MIT License
