# Sandbox

## Документы

* [Пользовательская инструкция](https://docs.google.com/document/d/1ShcHV4ls16ITBUpDKy0Cgma3YZSuzhwF_RsRUBFBALs/edit#heading=h.5l6922wptpom)
* [Функциональная матрица](https://docs.google.com/spreadsheets/d/1AHWRHaipZW2nGoYlFiEIInMZb8QQqLzLCMKsXChW038/edit#gid=1829484092)
* [Инструкция по записи сенсора](https://docs.google.com/document/d/1QL1rqFc2gmlIOgYpQnrWraDWFhEmJhP4w6LWEJX0Pro/edit?usp=sharing)

## Требования

### Windows

Для работы sandbox с Kinect 2 требуется одно из:
* Kinect for Windows Runtime 2.0 (https://www.microsoft.com/en-us/download/details.aspx?id=44559 или https://www.microsoft.com/en-us/download/details.aspx?id=44561)
* UsbDk или libusbK для работы через OpenNI2 + libfreenect2. Инструкция по установке: https://github.com/OpenKinect/libfreenect2/blob/master/README.md#windows--visual-studio
* Логи - C:\Users\[user]\AppData\LocalLow\Darkkon\sandbox\Player.log
* Записи - C:\Users\[user]\AppData\LocalLow\Darkkon\SandboxRecords

### Linux

* Чтобы использовать встроенные библиотеки OpenNI2 с драйверами, сборку следует запускать через `run.sh`
* OpenNI2 + libfreenect2 (https://github.com/OpenKinect/libfreenect2/blob/master/README.md#linux). В случае ArchLinux быстрее всего поставить из AUR (https://aur.archlinux.org/packages/libfreenect2-git)
* OpenNI2 + librealsense (https://github.com/IntelRealSense/librealsense/tree/development/wrappers/openni2). ArchLinux AUR - https://aur.archlinux.org/packages/librealsense 
* Логи - ~/.config/unity3d/Darkkon/sandbox/Player.log
* Записи - ~/.config/unity3d/Darkkon/SandboxRecords

### Дополнительные параметры запуска

`-r <Путь>`, `--record <Путь>` - Вместо подключения к устройству сразу окрыть запись по указанному пути (наличие устройства не требуется).  

## Разработка

Минимальная версия Unity - 2019.3

Тестирование в редакторе пока омрачется следующими проблемами - https://github.com/BrandyMint/sandbox/issues?q=is%3Aissue+is%3Aopen+label%3A%22editor+bug%22.

Эти проблемы не воспроизводятся в сборках.

Версионирование согласно [Semantic Versioning 2.0](https://semver.org)
