# Sandbox

## Требования

Проектор должен быть подключен как второй расширенный экран.
Монитор компьютера должен быть главным экраном.

Инструкция - https://docs.google.com/document/d/1ShcHV4ls16ITBUpDKy0Cgma3YZSuzhwF_RsRUBFBALs/edit#heading=h.5l6922wptpom

### Windows

Для работы sandbox с Kinect 2 требуется одно из:
* Kinect for Windows Runtime 2.0 (https://www.microsoft.com/en-us/download/details.aspx?id=44559 или https://www.microsoft.com/en-us/download/details.aspx?id=44561)
* UsbDk или libusbK для работы через OpenNI2 + libfreenect2. Инструкция по установке: https://github.com/OpenKinect/libfreenect2/blob/master/README.md#windows--visual-studio

### Linux

* Чтобы использовать встроенные библиотеки OpenNI2 с драйверами, сборку следует запускать через `run.sh`
* Графическая оболочка системы должна быть на X11, так как только для него реализован обход бага Unity с несколькими экранами.
* OpenNI2 + libfreenect2 (https://github.com/OpenKinect/libfreenect2/blob/master/README.md#linux). В случае ArchLinux быстрее всего поставить из AUR (https://aur.archlinux.org/packages/libfreenect2-git)
* OpenNI2 + librealsense (https://github.com/IntelRealSense/librealsense/tree/development/wrappers/openni2). ArchLinux AUR - https://aur.archlinux.org/packages/librealsense 
     
## Разработка

Минимальная версия Unity - 2019.2

Тестирование в редакторе пока омрачется следующими проблемами - https://github.com/BrandyMint/sandbox/issues?q=is%3Aissue+is%3Aopen+label%3A%22editor+bug%22.

Эти проблемы не воспроизводятся в сборках.
