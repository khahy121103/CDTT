// some scripts

// jquery ready start
$(document).ready(function () {
    console.log("Countdown script loaded!"); // Đặt log để kiểm tra file đúng

    // Countdown logic
    const countdownDate = new Date("2025-04-30T23:59:59").getTime();

    function updateCountdown() {
        const now = new Date().getTime();
        const timeLeft = countdownDate - now;

        const days = Math.floor(timeLeft / (1000 * 60 * 60 * 24));
        const hours = Math.floor((timeLeft % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((timeLeft % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((timeLeft % (1000 * 60)) / 1000);

        console.log({ days, hours, minutes, seconds }); // Kiểm tra giá trị

        $("#days").text(days);
        $("#hours").text(hours < 10 ? "0" + hours : hours);
        $("#minutes").text(minutes < 10 ? "0" + minutes : minutes);
        $("#seconds").text(seconds < 10 ? "0" + seconds : seconds);

        if (timeLeft < 0) {
            clearInterval(timerInterval);
            $("#days, #hours, #minutes, #seconds").text("00");
        }
    }

    const timerInterval = setInterval(updateCountdown, 1000);
    updateCountdown(); // Khởi chạy ngay khi DOM load
	// jQuery code

    
  // var html_download = '<a href="http://bootstrap-ecommerce.com/templates.html" class="btn btn-dark rounded-pill" style="font-size:13px; z-index:100; position: fixed; bottom:10px; right:10px;">Download theme</a>';
  //  $('body').prepend(html_download);
    

	//////////////////////// Prevent closing from click inside dropdown
    $(document).on('click', '.dropdown-menu', function (e) {
      e.stopPropagation();
    });


    

	//////////////////////// Bootstrap tooltip
	if($('[data-toggle="tooltip"]').length>0) {  // check if element exists
		$('[data-toggle="tooltip"]').tooltip()
	} // end if




    
}); 
// jquery end

