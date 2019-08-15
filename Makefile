UNITY=/home/konstantin/Unity/2019.3.0a12/Editor/Unity
PROJECT=./
OUT=./Builds
FLAGS=-quit -batchmode -nographics -projectPath $(PROJECT)
EXEC=-executeMethod BuildHelper.Editor.UserBuildCommands


all: build

build:
	$(UNITY) $(FLAGS) $(EXEC).Build -outputPath $(OUT)

buildWin64:
	$(UNITY) $(FLAGS) $(EXEC).BuildWin64 -outputPath $(OUT)

buildLinux:
	$(UNITY) $(FLAGS) $(EXEC).BuildLinux -outputPath $(OUT)

runTests:
	$(UNITY) $(FLAGS) -runEditorTests
