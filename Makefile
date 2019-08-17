ifeq ($(OS),Windows_NT)
else
	UNAME_S := $(shell uname -s)
	ifeq ($(UNAME_S),Linux)		
		UNITY_PATH_SEARCH=~/Unity
		UNITY=$(UNITY_PATH_SEARCH)/$(shell ls -1v $(UNITY_PATH_SEARCH) | tail --lines=1)/Editor/Unity
	endif
endif
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
