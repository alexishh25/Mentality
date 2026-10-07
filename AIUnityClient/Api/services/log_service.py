class LogService:

    def print_event(self,event):

        print("-------------------")
        print(event.category)
        print(event.message)
        print("-------------------")