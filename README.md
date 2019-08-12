# Sandbox

## Требования

Проектор должен быть подключен как второй расширенный экран.
Монитор компьютера должен быть главным экраном.

### Windows

Для работы sandbox с Kinect 2 требуется одно из:
* Kinect for Windows Runtime 2.0 (https://www.microsoft.com/en-us/download/details.aspx?id=44559 или https://www.microsoft.com/en-us/download/details.aspx?id=44561)
* UsbDk или libusbK для работы через OpenNI2 + libfreenect2. Инструкция по установке: https://github.com/OpenKinect/libfreenect2/blob/master/README.md#windows--visual-studio. А также в сборке нужно объявить `ENABLE_OPENNI2`

### Linux

* Графическая оболочка системы должна быть на X11, так как только для него реализован обход бага Unity с несколькими экранами.
* OpenNI2 + libfreenect2 (https://github.com/OpenKinect/libfreenect2/blob/master/README.md#linux). В случае ArchLinux быстрее всего поставить из AUR (https://aur.archlinux.org/packages/libfreenect2-git).

Вообще при объявленном `ENABLE_OPENNI2` в сборку входят свои библиотеки OpenNI2 + libfreenect2. Но нужно проверить что они подхватываются программой.
     
## Разработка

Минимальная версия Unity - 2019.2

Тестирование в редакторе пока омрачется следующими проблемами - https://github.com/BrandyMint/sandbox/issues?q=is%3Aissue+is%3Aopen+label%3A%22editor+bug%22.

Эти проблемы не воспроизводятся в сборках.